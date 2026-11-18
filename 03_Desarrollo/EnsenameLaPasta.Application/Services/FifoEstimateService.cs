using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Application.Common.Options;
using EnsenameLaPasta.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Application.Services;

/// <summary>
/// Estimación de ganancia patrimonial FIFO (HV-059). Trabaja en la divisa nativa del instrumento
/// (no convierte a divisa base): es lo relevante para la autoliquidación de Hacienda, que se
/// declara por operación en su propia divisa.
/// </summary>
public sealed class FifoEstimateService(ITradingDbContext db, IOptions<BrokerOptions> broker) : IFifoEstimateService
{
    private readonly ITradingDbContext _db = db;
    private readonly IOptions<BrokerOptions> _broker = broker;

    public async Task<FifoEstimateDto> EstimateAsync(
        string symbol,
        decimal quantity,
        decimal? exitPrice = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity debe ser > 0.");

        var normalized = symbol.Trim().ToUpperInvariant();

        // Lotes abiertos del símbolo, del más antiguo al más reciente (FIFO).
        var openLots = await _db.Trades
            .Where(t => t.Symbol == normalized && t.Status == TradeStatus.Open)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var currency = await _db.TrackedSymbols
            .Where(t => t.Symbol == normalized)
            .Select(t => t.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        if (openLots.Count == 0)
        {
            return new FifoEstimateDto(normalized, currency, quantity, 0m, exitPrice ?? 0m, 0m, 0m, 0m, 0m,
                [], $"No hay posiciones abiertas de {normalized}.");
        }

        // Precio de venta: el indicado o, por defecto, el último tick conocido; si tampoco hay
        // ticks (símbolo recién añadido, sin feed todavía), se usa el precio del primer lote FIFO
        // consumido como referencia honesta (degrada sin romper, igual que el resto del proyecto).
        decimal resolvedExitPrice;
        string? message = null;
        if (exitPrice.HasValue && exitPrice.Value > 0)
        {
            resolvedExitPrice = exitPrice.Value;
        }
        else
        {
            var lastTickPrice = await _db.MarketTicks
                .Where(t => t.Symbol == normalized)
                .OrderByDescending(t => t.Timestamp)
                .Select(t => (decimal?)t.Price)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastTickPrice.HasValue)
            {
                resolvedExitPrice = lastTickPrice.Value;
            }
            else
            {
                resolvedExitPrice = openLots[0].EntryPrice;
                message = "Sin precio de mercado disponible; se usó el precio de entrada del primer lote como referencia.";
            }
        }

        var lots = new List<FifoLotConsumptionDto>();
        var remaining = quantity;
        decimal totalCostBasis = 0m, totalProceeds = 0m;

        foreach (var lot in openLots)
        {
            if (remaining <= 0m) break;

            var take = Math.Min(remaining, lot.Quantity);
            // Comisión de compra prorrateada a la porción del lote realmente vendida.
            var proratedBuyCommission = lot.Quantity > 0m ? lot.Commission * (take / lot.Quantity) : 0m;
            var costBasis = take * lot.EntryPrice + proratedBuyCommission;
            var proceeds = take * resolvedExitPrice;

            lots.Add(new FifoLotConsumptionDto(
                lot.Id, lot.CreatedAt, take, lot.EntryPrice, costBasis, proceeds, proceeds - costBasis));

            totalCostBasis += costBasis;
            totalProceeds += proceeds;
            remaining -= take;
        }

        var quantityMatched = quantity - remaining;
        if (remaining > 0m)
        {
            message = $"Solo había {quantityMatched:0.######} unidades abiertas de {normalized}; "
                + $"se han ignorado las {remaining:0.######} restantes de la cantidad solicitada.";
        }

        // Comisión fija por orden (Trade Republic: 1 orden de venta, independiente de cuántos
        // lotes FIFO se consuman) — HV-050. Solo se aplica si realmente hay algo que vender.
        var sellCommission = quantityMatched > 0m ? _broker.Value.CommissionPerOrder : 0m;
        var estimatedGain = totalProceeds - totalCostBasis - sellCommission;

        return new FifoEstimateDto(
            normalized, currency, quantity, quantityMatched, resolvedExitPrice, sellCommission,
            totalCostBasis, totalProceeds, estimatedGain, lots, message);
    }
}

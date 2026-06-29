using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ITradingDbContext _db;
    private readonly IOptionsMonitor<MarketDataOptions> _marketData;

    public DashboardService(
        ITradingDbContext db,
        IOptionsMonitor<MarketDataOptions> marketData)
    {
        _db = db;
        _marketData = marketData;
    }

    public async Task<DashboardDto> GetSnapshotAsync(
        int priceSeriesPoints = 50,
        int recentClosedCount = 20,
        CancellationToken cancellationToken = default)
    {
        // NOTA: SQLite almacena decimal como TEXT (ver HV-003 / configurations).
        // Eso impide aritmética sobre decimal en la cláusula SQL.
        // Por eso traemos las entidades a memoria y agregamos en C#.

        // Trades cerrados (todos, para agregados; tabla muestra solo los últimos N)
        var allClosed = await _db.Trades
            .Where(t => t.Status == TradeStatus.Closed)
            .OrderByDescending(t => t.ClosedAt)
            .ToListAsync(cancellationToken);

        var realizedPnL = allClosed
            .Where(t => t.ExitPrice.HasValue)
            .Sum(t => (t.ExitPrice!.Value - t.EntryPrice) * t.Quantity);

        var closedCount = allClosed.Count;
        var winnersCount = allClosed.Count(t => t.ExitPrice.HasValue && t.ExitPrice.Value > t.EntryPrice);
        var winrate = closedCount > 0
            ? Math.Round((decimal)winnersCount / closedCount * 100m, 2)
            : 0m;

        var recentClosed = allClosed.Take(recentClosedCount).ToList();

        // Trades abiertos
        var openTrades = await _db.Trades
            .Where(t => t.Status == TradeStatus.Open)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        // Divisa de cotización por símbolo (watchlist). Vacía si aún no se conoce.
        var currencyBySymbol = await _db.TrackedSymbols
            .ToDictionaryAsync(t => t.Symbol, t => t.Currency, cancellationToken);

        // Último precio por símbolo (necesario para unrealized PnL)
        var symbols = openTrades.Select(t => t.Symbol).Distinct().ToList();
        var lastPriceBySymbol = new Dictionary<string, decimal>();
        foreach (var symbol in symbols)
        {
            var lastTick = await _db.MarketTicks
                .Where(t => t.Symbol == symbol)
                .OrderByDescending(t => t.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);
            if (lastTick is not null)
                lastPriceBySymbol[symbol] = lastTick.Price;
        }

        var openDtos = openTrades.Select(t =>
        {
            var current = lastPriceBySymbol.GetValueOrDefault(t.Symbol, t.EntryPrice);
            var unrealized = (current - t.EntryPrice) * t.Quantity;
            var pct = t.EntryPrice != 0m
                ? Math.Round((current - t.EntryPrice) / t.EntryPrice * 100m, 2)
                : 0m;
            var currency = currencyBySymbol.GetValueOrDefault(t.Symbol, string.Empty);
            return new OpenTradeDto(t.Id, t.Symbol, t.EntryPrice, current, t.Quantity, unrealized, pct, currency, t.CreatedAt);
        }).ToList();

        var unrealizedPnL = openDtos.Sum(o => o.UnrealizedPnL);

        // Capital derivado de las posiciones abiertas (cartera real, sin capital ficticio).
        var invested = openTrades.Sum(t => t.EntryPrice * t.Quantity);     // coste base
        var marketValue = openDtos.Sum(o => o.CurrentPrice * o.Quantity);  // valor a precio real

        // Caja: aportaciones netas + efecto de operaciones.
        // cash = aportado − invertido(abiertas) + realizado(cerradas)   (las ventas devuelven coste+PnL)
        // Importe se guarda como TEXT (decimal); sumar en memoria.
        var netDeposits = (await _db.CashMovements.Select(m => m.Amount).ToListAsync(cancellationToken)).Sum();
        var cash = netDeposits - invested + realizedPnL;
        var accountValue = cash + marketValue;

        // Rentabilidad sobre el aportado neto. Por el invariante accountValue = netDeposits + totalPnL,
        // esto es (accountValue − netDeposits) / netDeposits × 100. 0 % si no hay aportaciones.
        var totalPnL = realizedPnL + unrealizedPnL;
        var returnPct = netDeposits != 0m
            ? Math.Round(totalPnL / netDeposits * 100m, 2)
            : 0m;

        var closedDtos = recentClosed.Select(t =>
        {
            var pct = t.EntryPrice != 0m
                ? Math.Round((t.ExitPrice!.Value - t.EntryPrice) / t.EntryPrice * 100m, 2)
                : 0m;
            var currency = currencyBySymbol.GetValueOrDefault(t.Symbol, string.Empty);
            return new ClosedTradeDto(
                t.Id, t.Symbol, t.EntryPrice, t.ExitPrice!.Value, t.Quantity,
                (t.ExitPrice.Value - t.EntryPrice) * t.Quantity, pct, currency,
                t.CreatedAt, t.ClosedAt!.Value);
        }).ToList();

        // Serie de precios por símbolo
        var allSymbols = await _db.MarketTicks
            .Select(t => t.Symbol)
            .Distinct()
            .ToListAsync(cancellationToken);

        var priceSeries = new List<PriceSeriesDto>(allSymbols.Count);
        foreach (var symbol in allSymbols)
        {
            var points = await _db.MarketTicks
                .Where(t => t.Symbol == symbol)
                .OrderByDescending(t => t.Timestamp)
                .Take(priceSeriesPoints)
                .ToListAsync(cancellationToken);

            var ordered = points
                .OrderBy(t => t.Timestamp)
                .Select(t => new PricePoint(t.Timestamp, t.Price))
                .ToList();

            priceSeries.Add(new PriceSeriesDto(symbol, ordered));
        }

        // Estado del feed: proveedor activo, total de ticks y último tick.
        var totalTicks = await _db.MarketTicks.CountAsync(cancellationToken);
        var lastTickUtc = await _db.MarketTicks
            .OrderByDescending(t => t.Timestamp)
            .Select(t => (DateTime?)t.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
        var dbSizeBytes = await _db.GetDatabaseSizeBytesAsync(cancellationToken);

        return new DashboardDto
        {
            ProviderType = _marketData.CurrentValue.ProviderType,
            TotalTicks = totalTicks,
            LastTickUtc = lastTickUtc,
            DatabaseSizeBytes = dbSizeBytes,
            Invested = invested,
            MarketValue = marketValue,
            NetDeposits = netDeposits,
            Cash = cash,
            AccountValue = accountValue,
            ReturnPct = returnPct,
            RealizedPnL = realizedPnL,
            UnrealizedPnL = unrealizedPnL,
            TotalPnL = totalPnL,
            OpenPositions = openTrades.Count,
            ClosedTrades = closedCount,
            Winrate = winrate,
            OpenTrades = openDtos,
            RecentClosedTrades = closedDtos,
            PriceSeries = priceSeries
        };
    }

    public async Task<IReadOnlyList<AccountHistoryPointDto>> GetAccountHistoryAsync(
        int maxPoints = 500,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(maxPoints, 1, 5000);

        // Decimal se almacena como TEXT en SQLite (HV-003): traemos a memoria y derivamos en C#.
        // Tomamos los más recientes (descendente) y luego invertimos a orden cronológico.
        var snapshots = await _db.PortfolioSnapshots
            .OrderByDescending(s => s.Timestamp)
            .Take(take)
            .ToListAsync(cancellationToken);

        snapshots.Reverse();

        return snapshots.Select(s =>
        {
            var totalPnL = s.RealizedPnL + s.UnrealizedPnL;
            var netDeposits = s.Capital - totalPnL;
            var returnPct = netDeposits != 0m
                ? Math.Round(totalPnL / netDeposits * 100m, 2)
                : 0m;
            return new AccountHistoryPointDto(s.Timestamp, s.Capital, netDeposits, totalPnL, returnPct);
        }).ToList();
    }
}

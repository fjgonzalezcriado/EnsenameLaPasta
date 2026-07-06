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
    private readonly IMarketProviderState _providerState;
    private readonly IFxRateProvider _fx;
    private readonly IOptions<FxOptions> _fxOptions;

    public DashboardService(
        ITradingDbContext db,
        IMarketProviderState providerState,
        IFxRateProvider fx,
        IOptions<FxOptions> fxOptions)
    {
        _db = db;
        _providerState = providerState;
        _fx = fx;
        _fxOptions = fxOptions;
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

        // Conversión a divisa base (HV-020/021/022): cada posición/aportación se convierte
        // con el tipo de su divisa. Las filas muestran su divisa nativa (HV-019) y, además,
        // el PnL convertido a base por fila (HV-022). Se prepara aquí para usarlo al construir las filas.
        var baseCurrency = (_fxOptions.Value.BaseCurrency ?? "EUR").Trim().ToUpperInvariant();

        string Norm(string? c)
        {
            var v = (c ?? string.Empty).Trim().ToUpperInvariant();
            return v.Length == 0 ? baseCurrency : v;   // desconocida/vacía → se asume base (rate 1)
        }
        string CcyOf(string symbol) => Norm(currencyBySymbol.GetValueOrDefault(symbol, string.Empty));

        // Movimientos de caja con su divisa (las aportaciones pueden ser en distintas monedas).
        var cashMovements = await _db.CashMovements
            .Select(m => new { m.Amount, m.Currency })
            .ToListAsync(cancellationToken);

        var distinctCcy = openTrades.Select(t => t.Symbol)
            .Concat(allClosed.Select(t => t.Symbol))
            .Select(CcyOf)
            .Concat(cashMovements.Select(m => Norm(m.Currency)))
            .Distinct()
            .ToList();

        var rateByCcy = new Dictionary<string, decimal>(distinctCcy.Count);
        foreach (var ccy in distinctCcy)
            rateByCcy[ccy] = await _fx.GetRateAsync(ccy, baseCurrency, cancellationToken);

        decimal RateOf(string symbol) => rateByCcy.GetValueOrDefault(CcyOf(symbol), 1m);

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
            return new OpenTradeDto(t.Id, t.Symbol, t.EntryPrice, current, t.Quantity, unrealized, pct, currency, unrealized * RateOf(t.Symbol), t.CreatedAt);
        }).ToList();

        // Agregados convertidos a base (sin redondear, para preservar el invariante exacto).
        var realizedPnL = allClosed
            .Where(t => t.ExitPrice.HasValue)
            .Sum(t => (t.ExitPrice!.Value - t.EntryPrice) * t.Quantity * RateOf(t.Symbol));

        var unrealizedPnL = openDtos.Sum(o => o.UnrealizedPnL * RateOf(o.Symbol));

        // Capital derivado de las posiciones abiertas (cartera real, sin capital ficticio).
        var invested = openTrades.Sum(t => t.EntryPrice * t.Quantity * RateOf(t.Symbol));      // coste base (en divisa base)
        var marketValue = openDtos.Sum(o => o.CurrentPrice * o.Quantity * RateOf(o.Symbol));   // valor a precio real (en divisa base)

        // Caja: aportaciones netas + efecto de operaciones.
        // cash = aportado − invertido(abiertas) + realizado(cerradas)   (las ventas devuelven coste+PnL)
        // Cada aportación se convierte a la divisa base por su propia divisa (HV-021).
        var netDeposits = cashMovements.Sum(m => m.Amount * rateByCcy.GetValueOrDefault(Norm(m.Currency), 1m));
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
            var realized = (t.ExitPrice!.Value - t.EntryPrice) * t.Quantity;
            return new ClosedTradeDto(
                t.Id, t.Symbol, t.EntryPrice, t.ExitPrice.Value, t.Quantity,
                realized, pct, currency, realized * RateOf(t.Symbol),
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
                .Select(t => new PricePoint(t.Timestamp, t.Price, t.Volume))
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
            ProviderType = _providerState.Current,
            TotalTicks = totalTicks,
            LastTickUtc = lastTickUtc,
            DatabaseSizeBytes = dbSizeBytes,
            Invested = invested,
            MarketValue = marketValue,
            NetDeposits = netDeposits,
            Cash = cash,
            AccountValue = accountValue,
            BaseCurrency = baseCurrency,
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

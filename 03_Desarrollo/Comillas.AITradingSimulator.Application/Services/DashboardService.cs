using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Application.Services;

public sealed class DashboardService(
    ITradingDbContext db,
    IMarketProviderState providerState,
    IFxRateProvider fx,
    IOptions<FxOptions> fxOptions) : IDashboardService
{
    private readonly ITradingDbContext _db = db;
    private readonly IMarketProviderState _providerState = providerState;
    private readonly IFxRateProvider _fx = fx;
    private readonly IOptions<FxOptions> _fxOptions = fxOptions;

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

        // Dividendos cobrados con su divisa (HV-050): suman al PnL realizado y al efectivo.
        var dividends = await _db.Dividends
            .Select(d => new { d.Amount, d.Currency })
            .ToListAsync(cancellationToken);

        var distinctCcy = openTrades.Select(t => t.Symbol)
            .Concat(allClosed.Select(t => t.Symbol))
            .Select(CcyOf)
            .Concat(cashMovements.Select(m => Norm(m.Currency)))
            .Concat(dividends.Select(d => Norm(d.Currency)))
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

        // Comisiones del bróker (HV-050), ya en divisa base (se cobran en EUR). Las cerradas llevan
        // 2× (compra+venta); las abiertas 1× (compra). Los dividendos (base) suman a lo realizado.
        var commissionsClosed = allClosed.Sum(t => t.Commission);
        var commissionsOpen = openTrades.Sum(t => t.Commission);
        var dividendsBase = dividends.Sum(d => d.Amount * rateByCcy.GetValueOrDefault(Norm(d.Currency), 1m));

        // Agregados convertidos a base (sin redondear, para preservar el invariante exacto).
        var realizedGross = allClosed
            .Where(t => t.ExitPrice.HasValue)
            .Sum(t => (t.ExitPrice!.Value - t.EntryPrice) * t.Quantity * RateOf(t.Symbol));
        // Realizado neto = bruto de ventas − comisiones de cerradas + dividendos cobrados.
        var realizedPnL = realizedGross - commissionsClosed + dividendsBase;

        // No realizado neto = bruto de abiertas − comisiones de compra ya pagadas.
        var unrealizedGross = openDtos.Sum(o => o.UnrealizedPnL * RateOf(o.Symbol));
        var unrealizedPnL = unrealizedGross - commissionsOpen;

        // Capital derivado de las posiciones abiertas (cartera real, sin capital ficticio).
        var invested = openTrades.Sum(t => t.EntryPrice * t.Quantity * RateOf(t.Symbol));      // coste base (en divisa base)
        var marketValue = openDtos.Sum(o => o.CurrentPrice * o.Quantity * RateOf(o.Symbol));   // valor a precio real (en divisa base)

        // Caja: aportaciones netas + efecto de operaciones (comisiones y dividendos incluidos).
        // cash = aportado − invertido(abiertas) − comisiones de compra + realizado(neto de cerradas+dividendos).
        // Invariante preservado: accountValue = netDeposits + totalPnL.
        var netDeposits = cashMovements.Sum(m => m.Amount * rateByCcy.GetValueOrDefault(Norm(m.Currency), 1m));
        var cash = netDeposits - invested - commissionsOpen + realizedPnL;
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

        return [.. snapshots.Select(s =>
        {
            var totalPnL = s.RealizedPnL + s.UnrealizedPnL;
            var netDeposits = s.Capital - totalPnL;
            var returnPct = netDeposits != 0m
                ? Math.Round(totalPnL / netDeposits * 100m, 2)
                : 0m;
            return new AccountHistoryPointDto(s.Timestamp, s.Capital, netDeposits, totalPnL, returnPct);
        })];
    }

    public async Task<PortfolioMetricsDto> GetPortfolioMetricsAsync(CancellationToken cancellationToken = default)
    {
        // Curva de capital (ascendente). Decimal→TEXT en SQLite: se computa en C#.
        var snapshots = await _db.PortfolioSnapshots
            .OrderByDescending(s => s.Timestamp)
            .Take(5000)
            .ToListAsync(cancellationToken);
        snapshots.Reverse();

        // Profit factor de trades cerrados (independiente de los snapshots).
        // Decimal→TEXT en SQLite: materializamos y usamos RealizedPnL (computado en C#).
        var closedTrades = await _db.Trades
            .Where(t => t.Status == TradeStatus.Closed)
            .ToListAsync(cancellationToken);
        // PnL neto de comisiones por trade (HV-050).
        var closedPnls = closedTrades.Select(t => (t.RealizedPnL ?? 0m) - t.Commission).ToList();
        var gains = closedPnls.Where(p => p > 0m).Sum();
        var losses = closedPnls.Where(p => p < 0m).Sum(p => -p);
        var pfInfinite = losses == 0m && gains > 0m;
        var profitFactor = losses > 0m ? Math.Round(gains / losses, 2) : 0m;
        var closedCount = closedPnls.Count;

        var count = snapshots.Count;
        if (count < 2)
        {
            return new PortfolioMetricsDto(false, count, 0, count > 0 ? snapshots[^1].Capital : 0m,
                count > 0 ? snapshots[^1].Capital : 0m, 0m, 0m, 0, 0, profitFactor, pfInfinite, closedCount,
                "Aún no hay suficientes snapshots del valor de cuenta para las métricas.");
        }

        // Índice de retorno (1 + PnL/aportado) por snapshot: NEUTRAL a aportaciones/retiradas
        // (un ingreso de caja no cuenta como rentabilidad). Las métricas se calculan sobre él,
        // no sobre el valor de cuenta bruto (que se dispararía con cada aportación).
        var indexed = snapshots.Select(s =>
        {
            var totalPnL = s.RealizedPnL + s.UnrealizedPnL;
            var netDep = s.Capital - totalPnL;
            var retPct = netDep != 0m ? totalPnL / netDep : 0m;   // fracción
            var idx = 1.0 + (double)retPct;
            return (s.Timestamp, Index: idx > 0 ? idx : 0.0001);   // guarda de positividad
        }).ToList();

        // Drawdown del índice de retorno: peor caída pico→valle y caída actual desde el pico.
        double peakIdx = double.MinValue, maxDd = 0;
        foreach (var p in indexed)
        {
            if (p.Index > peakIdx) peakIdx = p.Index;
            if (peakIdx > 0) { var dd = (p.Index - peakIdx) / peakIdx; if (dd < maxDd) maxDd = dd; }
        }
        var currentDd = peakIdx > 0 ? (indexed[^1].Index - peakIdx) / peakIdx : 0.0;

        // Valor de cuenta (pico / actual) para contexto informativo.
        var peak = snapshots.Max(s => s.Capital);
        var current = snapshots[^1].Capital;

        // Sharpe / volatilidad sobre retornos DIARIOS del índice (último de cada día UTC), √252.
        var daily = indexed
            .GroupBy(p => p.Timestamp.Date)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(p => p.Timestamp).Last().Index)
            .ToList();
        var returns = new List<double>();
        for (var i = 1; i < daily.Count; i++)
            if (daily[i - 1] != 0) returns.Add(daily[i] / daily[i - 1] - 1.0);

        // Sharpe/volatilidad no son interpretables con muy pocos datos (anualizar 2-3 retornos
        // da valores absurdos). Exigimos ≥ 10 retornos diarios (~2 semanas de sesiones).
        var hasEnough = returns.Count >= 10;
        double sharpe = 0, volPct = 0;
        if (hasEnough)
        {
            var mean = returns.Average();
            var sd = Math.Sqrt(returns.Sum(r => (r - mean) * (r - mean)) / returns.Count);
            volPct = Math.Round(sd * Math.Sqrt(252) * 100.0, 2);
            sharpe = sd > 0 ? Math.Round(mean / sd * Math.Sqrt(252), 2) : 0;
        }

        return new PortfolioMetricsDto(
            hasEnough, count, daily.Count, peak, current,
            Math.Round((decimal)(maxDd * 100.0), 2), Math.Round((decimal)(currentDd * 100.0), 2),
            sharpe, volPct, profitFactor, pfInfinite, closedCount,
            hasEnough ? null : $"Sharpe/volatilidad necesitan ≥ 11 días de snapshots (hay {daily.Count}).");
    }

    public async Task<ClosedTradesBreakdownDto> GetClosedTradesBreakdownAsync(CancellationToken cancellationToken = default)
    {
        var closed = await _db.Trades
            .Where(t => t.Status == TradeStatus.Closed && t.ClosedAt != null && t.ExitPrice != null)
            .ToListAsync(cancellationToken);

        var currencyBySymbol = await _db.TrackedSymbols
            .ToDictionaryAsync(t => t.Symbol, t => t.Currency, cancellationToken);
        var baseCurrency = (_fxOptions.Value.BaseCurrency ?? "EUR").Trim().ToUpperInvariant();
        string Norm(string? c) { var v = (c ?? string.Empty).Trim().ToUpperInvariant(); return v.Length == 0 ? baseCurrency : v; }
        string CcyOf(string symbol) => Norm(currencyBySymbol.GetValueOrDefault(symbol, string.Empty));

        var dividends = await _db.Dividends
            .Select(d => new { d.Amount, d.Currency, d.ReceivedAt })
            .ToListAsync(cancellationToken);

        // Tipos de cambio de las divisas presentes (trades + dividendos) → base.
        var ccys = closed.Select(t => CcyOf(t.Symbol)).Concat(dividends.Select(d => Norm(d.Currency))).Distinct();
        var rateByCcy = new Dictionary<string, decimal>();
        foreach (var ccy in ccys)
            rateByCcy[ccy] = await _fx.GetRateAsync(ccy, baseCurrency, cancellationToken);
        decimal RateOf(string symbol) => rateByCcy.GetValueOrDefault(CcyOf(symbol), 1m);

        // Acumulador por (año, mes): PnL neto de trades (bruto − comisión) y dividendos (base).
        var acc = new Dictionary<(int Year, int Month), (decimal Trades, decimal Div, int Count, int Wins, int Losses)>();
        (decimal, decimal, int, int, int) Get((int, int) k) => acc.TryGetValue(k, out var v) ? v : (0m, 0m, 0, 0, 0);

        foreach (var t in closed)
        {
            var net = (t.ExitPrice!.Value - t.EntryPrice) * t.Quantity * RateOf(t.Symbol) - t.Commission;
            var k = (t.ClosedAt!.Value.Year, t.ClosedAt!.Value.Month);
            var v = Get(k);
            acc[k] = (v.Item1 + net, v.Item2, v.Item3 + 1, v.Item4 + (net > 0m ? 1 : 0), v.Item5 + (net < 0m ? 1 : 0));
        }
        foreach (var d in dividends)
        {
            var div = d.Amount * rateByCcy.GetValueOrDefault(Norm(d.Currency), 1m);
            var k = (d.ReceivedAt.Year, d.ReceivedAt.Month);
            var v = Get(k);
            acc[k] = (v.Item1, v.Item2 + div, v.Item3, v.Item4, v.Item5);
        }

        var years = acc
            .GroupBy(kv => kv.Key.Year)
            .OrderByDescending(g => g.Key)
            .Select(yg =>
            {
                var months = yg
                    .OrderBy(kv => kv.Key.Month)
                    .Select(kv => new MonthBreakdownDto(
                        kv.Key.Month, Math.Round(kv.Value.Trades + kv.Value.Div, 2), Math.Round(kv.Value.Div, 2),
                        kv.Value.Count, kv.Value.Wins, kv.Value.Losses))
                    .ToList();
                return new YearBreakdownDto(
                    yg.Key,
                    Math.Round(yg.Sum(kv => kv.Value.Trades + kv.Value.Div), 2),
                    Math.Round(yg.Sum(kv => kv.Value.Div), 2),
                    yg.Sum(kv => kv.Value.Count), yg.Sum(kv => kv.Value.Wins), yg.Sum(kv => kv.Value.Losses),
                    months);
            })
            .ToList();

        var totalPnl = acc.Values.Sum(v => v.Trades + v.Div);
        var totalDiv = acc.Values.Sum(v => v.Div);
        var totalTrades = acc.Values.Sum(v => v.Count);
        return new ClosedTradesBreakdownDto(baseCurrency, Math.Round(totalPnl, 2), Math.Round(totalDiv, 2), totalTrades, years);
    }
}

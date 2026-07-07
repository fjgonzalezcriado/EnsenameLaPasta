using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Application.Services;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Tests.Application;

public sealed class DashboardServiceTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public DashboardServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new TradingDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private TradingDbContext NewContext() => new(_options);

    // FX por defecto: identidad (rate 1) → los tests sin divisa no cambian.
    private static DashboardService NewService(TradingDbContext ctx)
        => NewService(ctx, new StubFxRateProvider());

    private static DashboardService NewService(TradingDbContext ctx, IFxRateProvider fx)
        => new(ctx, new StaticProviderState(), fx,
            Microsoft.Extensions.Options.Options.Create(new FxOptions { BaseCurrency = "EUR" }));

    [Fact]
    public async Task GetSnapshot_SinTrades_CapitalActualIgualAInicial()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);

        var snap = await svc.GetSnapshotAsync();

        Assert.Equal(0m, snap.Invested);
        Assert.Equal(0m, snap.MarketValue);
        Assert.Equal(0m, snap.RealizedPnL);
        Assert.Equal(0m, snap.UnrealizedPnL);
        Assert.Equal(0m, snap.TotalPnL);
        Assert.Equal(0, snap.OpenPositions);
        Assert.Equal(0, snap.ClosedTrades);
        Assert.Equal(0m, snap.Winrate);
        Assert.Empty(snap.OpenTrades);
        Assert.Empty(snap.RecentClosedTrades);
        Assert.Empty(snap.PriceSeries);
    }

    [Fact]
    public async Task GetSnapshot_ConTradeCerradoConGanancia_RealizedPnLPositivo()
    {
        await using (var ctx = NewContext())
        {
            var t = Trade.Open("AAPL", 100m, 10m, BaseTime);
            t.Close(110m, BaseTime.AddHours(1));
            ctx.Trades.Add(t);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = NewService(ctx2);

        var snap = await svc.GetSnapshotAsync();

        Assert.Equal(100m, snap.RealizedPnL);  // (110-100)*10
        Assert.Equal(0m, snap.MarketValue);    // sin posiciones abiertas
        Assert.Equal(0m, snap.Invested);
        Assert.Equal(1, snap.ClosedTrades);
        Assert.Equal(100m, snap.Winrate);  // 1/1 ganador
        Assert.Single(snap.RecentClosedTrades);
        Assert.Equal(100m, snap.RecentClosedTrades[0].RealizedPnL);
    }

    [Fact]
    public async Task GetSnapshot_ConTradeCerradoConPerdida_RealizedPnLNegativo()
    {
        await using (var ctx = NewContext())
        {
            var t = Trade.Open("AAPL", 100m, 10m, BaseTime);
            t.Close(90m, BaseTime.AddHours(1));
            ctx.Trades.Add(t);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = NewService(ctx2);

        var snap = await svc.GetSnapshotAsync();

        Assert.Equal(-100m, snap.RealizedPnL);  // (90-100)*10 = -100
        Assert.Equal(0m, snap.Winrate);  // 0/1 ganadores
    }

    [Fact]
    public async Task GetSnapshot_ConTradeAbierto_UsaUltimoTickParaUnrealized()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 5m, BaseTime));
            // 3 ticks de AAPL, último a 110
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 102m, 100m, BaseTime.AddMinutes(1)));
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 108m, 100m, BaseTime.AddMinutes(2)));
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 110m, 100m, BaseTime.AddMinutes(3)));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = NewService(ctx2);

        var snap = await svc.GetSnapshotAsync();

        Assert.Equal(1, snap.OpenPositions);
        Assert.Single(snap.OpenTrades);

        var open = snap.OpenTrades[0];
        Assert.Equal(110m, open.CurrentPrice);            // último tick
        Assert.Equal(50m, open.UnrealizedPnL);            // (110-100)*5 = 50
        Assert.Equal(50m, snap.UnrealizedPnL);
        Assert.Equal(500m, snap.Invested);                // 100 × 5 (coste base)
        Assert.Equal(550m, snap.MarketValue);             // 110 × 5 (valor real)
    }

    [Fact]
    public async Task GetSnapshot_Winrate_50PorCiento_CuandoMitadGanan()
    {
        await using (var ctx = NewContext())
        {
            // 2 ganadores
            var t1 = Trade.Open("AAPL", 100m, 1m, BaseTime);
            t1.Close(110m, BaseTime.AddHours(1));
            var t2 = Trade.Open("GOOG", 100m, 1m, BaseTime);
            t2.Close(105m, BaseTime.AddHours(1));

            // 2 perdedores
            var t3 = Trade.Open("BTCUSD", 100m, 1m, BaseTime);
            t3.Close(90m, BaseTime.AddHours(1));
            var t4 = Trade.Open("AAPL", 100m, 1m, BaseTime.AddHours(2));
            t4.Close(95m, BaseTime.AddHours(3));

            ctx.Trades.AddRange(t1, t2, t3, t4);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = NewService(ctx2);

        var snap = await svc.GetSnapshotAsync();

        Assert.Equal(4, snap.ClosedTrades);
        Assert.Equal(50m, snap.Winrate);
        // Total: +10 +5 -10 -5 = 0
        Assert.Equal(0m, snap.RealizedPnL);
    }

    [Fact]
    public async Task GetSnapshot_PriceSeries_DevuelveTodosLosSimbolosOrdenadosPorTimestamp()
    {
        await using (var ctx = NewContext())
        {
            for (int i = 0; i < 5; i++)
            {
                ctx.MarketTicks.Add(MarketTick.Create("AAPL", 100m + i, 1m, BaseTime.AddSeconds(i)));
                ctx.MarketTicks.Add(MarketTick.Create("GOOG", 200m + i, 1m, BaseTime.AddSeconds(i)));
            }
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = NewService(ctx2);

        var snap = await svc.GetSnapshotAsync(priceSeriesPoints: 10);

        Assert.Equal(2, snap.PriceSeries.Count);

        var aapl = snap.PriceSeries.First(s => s.Symbol == "AAPL");
        Assert.Equal(5, aapl.Points.Count);
        // Ordenados ascendente por timestamp
        for (int i = 1; i < aapl.Points.Count; i++)
        {
            Assert.True(aapl.Points[i].Timestamp >= aapl.Points[i - 1].Timestamp);
        }
        Assert.Equal(100m, aapl.Points[0].Price);
        Assert.Equal(104m, aapl.Points[4].Price);
    }

    [Fact]
    public async Task GetSnapshot_PriceSeries_LimitaAlNumeroPedido()
    {
        await using (var ctx = NewContext())
        {
            for (int i = 0; i < 20; i++)
            {
                ctx.MarketTicks.Add(MarketTick.Create("AAPL", 100m + i, 1m, BaseTime.AddSeconds(i)));
            }
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = NewService(ctx2);

        var snap = await svc.GetSnapshotAsync(priceSeriesPoints: 5);

        Assert.Single(snap.PriceSeries);
        Assert.Equal(5, snap.PriceSeries[0].Points.Count);
        // Devuelve los 5 últimos (precios 115..119) ordenados ascendente
        Assert.Equal(115m, snap.PriceSeries[0].Points[0].Price);
        Assert.Equal(119m, snap.PriceSeries[0].Points[4].Price);
    }

    [Fact]
    public async Task GetSnapshot_DerivaEfectivoYValorDeCuentaDeLasPosicionesYCaja()
    {
        await using (var ctx = NewContext())
        {
            // Aporta 10000 € de efectivo
            ctx.CashMovements.Add(CashMovement.Create(10_000m, "inicial", BaseTime));
            // Posición abierta: 100 × 5 = 500 invertido
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 5m, BaseTime));
            // Último precio 110 -> valor de mercado 550, PnL no realizado 50
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 110m, 100m, BaseTime.AddMinutes(1)));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2).GetSnapshotAsync();

        Assert.Equal(10_000m, snap.NetDeposits);
        Assert.Equal(500m, snap.Invested);
        Assert.Equal(550m, snap.MarketValue);
        Assert.Equal(50m, snap.UnrealizedPnL);
        Assert.Equal(9_500m, snap.Cash);          // 10000 − 500 invertido + 0 realizado
        Assert.Equal(10_050m, snap.AccountValue);  // 9500 efectivo + 550 cartera
        // Invariante: valor de cuenta = aportado + PnL total
        Assert.Equal(snap.NetDeposits + snap.TotalPnL, snap.AccountValue);
    }

    [Fact]
    public async Task GetSnapshot_ConGanancia_ReturnPctPositivo()
    {
        await using (var ctx = NewContext())
        {
            // Aporta 10000 €, posición 100 × 5 = 500 invertido, precio actual 110 -> PnL no real. 50
            ctx.CashMovements.Add(CashMovement.Create(10_000m, "inicial", BaseTime));
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 5m, BaseTime));
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 110m, 100m, BaseTime.AddMinutes(1)));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2).GetSnapshotAsync();

        Assert.Equal(50m, snap.TotalPnL);
        Assert.Equal(0.5m, snap.ReturnPct);   // 50 / 10000 × 100
        // Coherente con (AccountValue − NetDeposits) / NetDeposits × 100
        Assert.Equal(
            Math.Round((snap.AccountValue - snap.NetDeposits) / snap.NetDeposits * 100m, 2),
            snap.ReturnPct);
    }

    [Fact]
    public async Task GetSnapshot_ConPerdida_ReturnPctNegativo()
    {
        await using (var ctx = NewContext())
        {
            // Aporta 1000 €, trade cerrado con pérdida de 100 ((90-100)*10)
            ctx.CashMovements.Add(CashMovement.Create(1_000m, "inicial", BaseTime));
            var t = Trade.Open("AAPL", 100m, 10m, BaseTime);
            t.Close(90m, BaseTime.AddHours(1));
            ctx.Trades.Add(t);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2).GetSnapshotAsync();

        Assert.Equal(-100m, snap.TotalPnL);
        Assert.Equal(-10m, snap.ReturnPct);   // -100 / 1000 × 100
    }

    [Fact]
    public async Task GetSnapshot_SinAportaciones_ReturnPctCero()
    {
        await using var ctx = NewContext();
        var snap = await NewService(ctx).GetSnapshotAsync();

        Assert.Equal(0m, snap.NetDeposits);
        Assert.Equal(0m, snap.ReturnPct);   // sin división por cero
    }

    [Fact]
    public async Task GetSnapshot_PosicionAbierta_CalculaReturnPctPorFila()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 5m, BaseTime));
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 110m, 100m, BaseTime.AddMinutes(1)));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2).GetSnapshotAsync();

        Assert.Single(snap.OpenTrades);
        Assert.Equal(10m, snap.OpenTrades[0].ReturnPct);   // (110-100)/100 × 100
    }

    [Fact]
    public async Task GetSnapshot_TradeCerrado_CalculaReturnPctPorFila()
    {
        await using (var ctx = NewContext())
        {
            var t = Trade.Open("AAPL", 100m, 10m, BaseTime);
            t.Close(90m, BaseTime.AddHours(1));
            ctx.Trades.Add(t);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2).GetSnapshotAsync();

        Assert.Single(snap.RecentClosedTrades);
        Assert.Equal(-10m, snap.RecentClosedTrades[0].ReturnPct);   // (90-100)/100 × 100
    }

    [Fact]
    public async Task GetSnapshot_AdjuntaDivisaPorSimboloEnAbiertasYCerradas()
    {
        await using (var ctx = NewContext())
        {
            // Watchlist con divisas conocidas (selladas por el generador en runtime).
            var aapl = TrackedSymbol.Create("AAPL", "Apple", BaseTime);
            aapl.SetCurrency("usd");   // se normaliza a USD
            var hy = TrackedSymbol.Create("HY9H.F", "SK hynix", BaseTime);
            hy.SetCurrency("EUR");
            ctx.TrackedSymbols.AddRange(aapl, hy);

            // Abierta en AAPL (USD)
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 5m, BaseTime));
            // Cerrada en HY9H.F (EUR)
            var closed = Trade.Open("HY9H.F", 1360m, 5m, BaseTime);
            closed.Close(1400m, BaseTime.AddHours(1));
            ctx.Trades.Add(closed);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2).GetSnapshotAsync();

        Assert.Equal("USD", snap.OpenTrades.Single().Currency);
        Assert.Equal("EUR", snap.RecentClosedTrades.Single().Currency);
    }

    [Fact]
    public async Task GetSnapshot_SimboloSinDivisaConocida_CurrencyVacia()
    {
        await using (var ctx = NewContext())
        {
            // Sin TrackedSymbol para GOOG (o sin divisa sellada todavía).
            ctx.Trades.Add(Trade.Open("GOOG", 100m, 1m, BaseTime));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2).GetSnapshotAsync();

        Assert.Equal(string.Empty, snap.OpenTrades.Single().Currency);
    }

    [Fact]
    public async Task GetSnapshot_ConvierteTotalesAlaDivisaBase()
    {
        await using (var ctx = NewContext())
        {
            // Aporta 10000 € (base). Posición en AAPL (USD): 100×10 = 1000 USD invertido,
            // precio actual 120 → 1200 USD, PnL no realizado 200 USD.
            ctx.CashMovements.Add(CashMovement.Create(10_000m, "inicial", BaseTime));
            var aapl = TrackedSymbol.Create("AAPL", "Apple", BaseTime);
            aapl.SetCurrency("USD");
            ctx.TrackedSymbols.Add(aapl);
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 10m, BaseTime));
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 120m, 100m, BaseTime.AddMinutes(1)));
            await ctx.SaveChangesAsync();
        }

        // USD→EUR = 0,90
        var fx = new StubFxRateProvider(new() { [("USD", "EUR")] = 0.90m });

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2, fx).GetSnapshotAsync();

        Assert.Equal("EUR", snap.BaseCurrency);
        Assert.Equal(900m, snap.Invested);        // 1000 USD × 0,90
        Assert.Equal(1080m, snap.MarketValue);    // 1200 USD × 0,90
        Assert.Equal(180m, snap.UnrealizedPnL);   // 200 USD × 0,90
        Assert.Equal(0m, snap.RealizedPnL);
        Assert.Equal(9_100m, snap.Cash);          // 10000 − 900 invertido
        Assert.Equal(10_180m, snap.AccountValue); // 9100 + 1080 cartera
        // Invariante preservado tras la conversión.
        Assert.Equal(snap.NetDeposits + snap.TotalPnL, snap.AccountValue);
    }

    [Fact]
    public async Task GetSnapshot_AdjuntaPnLConvertidoABasePorFila()
    {
        await using (var ctx = NewContext())
        {
            var aapl = TrackedSymbol.Create("AAPL", "Apple", BaseTime);
            aapl.SetCurrency("USD");
            ctx.TrackedSymbols.Add(aapl);

            // Abierta: 100×10, precio actual 120 → PnL no realizado 200 USD.
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 10m, BaseTime));
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 120m, 100m, BaseTime.AddMinutes(1)));

            // Cerrada: 100→90 ×10 → PnL realizado −100 USD.
            var closed = Trade.Open("AAPL", 100m, 10m, BaseTime);
            closed.Close(90m, BaseTime.AddHours(1));
            ctx.Trades.Add(closed);
            await ctx.SaveChangesAsync();
        }

        var fx = new StubFxRateProvider(new() { [("USD", "EUR")] = 0.90m });

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2, fx).GetSnapshotAsync();

        var open = snap.OpenTrades.Single();
        Assert.Equal(200m, open.UnrealizedPnL);       // nativo USD
        Assert.Equal(180m, open.UnrealizedPnLBase);   // 200 × 0,90

        var closedDto = snap.RecentClosedTrades.Single();
        Assert.Equal(-100m, closedDto.RealizedPnL);
        Assert.Equal(-90m, closedDto.RealizedPnLBase); // −100 × 0,90
    }

    [Fact]
    public async Task GetSnapshot_ConvierteAportacionesPorSuDivisa()
    {
        await using (var ctx = NewContext())
        {
            ctx.CashMovements.Add(CashMovement.Create(10_000m, "eur", BaseTime, "EUR"));
            ctx.CashMovements.Add(CashMovement.Create(1_000m, "usd", BaseTime.AddHours(1), "USD"));
            await ctx.SaveChangesAsync();
        }

        // USD→EUR = 0,90
        var fx = new StubFxRateProvider(new() { [("USD", "EUR")] = 0.90m });

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2, fx).GetSnapshotAsync();

        Assert.Equal(10_900m, snap.NetDeposits);    // 10000 EUR + 1000 USD × 0,90
        Assert.Equal(10_900m, snap.AccountValue);   // sin posiciones → cash = aportado
        Assert.Equal("EUR", snap.BaseCurrency);
    }

    [Fact]
    public async Task GetAccountHistory_SinSnapshots_DevuelveVacio()
    {
        await using var ctx = NewContext();
        var history = await NewService(ctx).GetAccountHistoryAsync();
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetAccountHistory_OrdenaAscendenteYDerivaNetDepositsYReturnPct()
    {
        await using (var ctx = NewContext())
        {
            // Insertados en orden no cronológico para verificar el orden de salida.
            // Capital = valor de cuenta; TotalPnL = realized + unrealized; NetDeposits = Capital − TotalPnL.
            ctx.PortfolioSnapshots.Add(PortfolioSnapshot.Create(
                BaseTime.AddMinutes(10), 10_050m, 0m, 50m, 1, 0)); // PnL 50, aportado 10000 -> +0,5%
            ctx.PortfolioSnapshots.Add(PortfolioSnapshot.Create(
                BaseTime, 10_000m, 0m, 0m, 0, 0));                 // aportado 10000, PnL 0 -> 0%
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var history = await NewService(ctx2).GetAccountHistoryAsync();

        Assert.Equal(2, history.Count);
        // Orden ascendente por timestamp
        Assert.True(history[0].Timestamp < history[1].Timestamp);

        Assert.Equal(10_000m, history[0].AccountValue);
        Assert.Equal(10_000m, history[0].NetDeposits);
        Assert.Equal(0m, history[0].ReturnPct);

        Assert.Equal(10_050m, history[1].AccountValue);
        Assert.Equal(50m, history[1].TotalPnL);
        Assert.Equal(10_000m, history[1].NetDeposits);   // 10050 − 50
        Assert.Equal(0.5m, history[1].ReturnPct);        // 50 / 10000 × 100
    }

    [Fact]
    public async Task GetPortfolioMetrics_CalculaDrawdownSobreIndiceDeRetorno()
    {
        await using (var ctx = NewContext())
        {
            // Aportado neto constante (100) y PnL variable → índice de retorno 1 → 1.2 → 0.9 → 1.1.
            // Capital = 100 + PnL; unrealized = Capital − 100 ⇒ NetDeposits = 100 constante.
            decimal[] caps = [100m, 120m, 90m, 110m];
            for (var i = 0; i < caps.Length; i++)
                ctx.PortfolioSnapshots.Add(PortfolioSnapshot.Create(BaseTime.AddDays(i), caps[i], 0m, caps[i] - 100m, 0, 0));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var m = await NewService(ctx2).GetPortfolioMetricsAsync();

        Assert.Equal(4, m.SnapshotCount);
        Assert.Equal(120m, m.PeakValue);
        Assert.Equal(-25.00m, m.MaxDrawdownPct);     // (0.9 − 1.2) / 1.2
        Assert.Equal(-8.33m, m.CurrentDrawdownPct);  // (1.1 − 1.2) / 1.2
        Assert.False(m.HasEnoughData);               // 3 retornos < 10 → Sharpe/vol insuficientes
        Assert.NotNull(m.Message);
    }

    [Fact]
    public async Task GetPortfolioMetrics_ConSuficientesDias_CalculaSharpeYVolatilidad()
    {
        await using (var ctx = NewContext())
        {
            // 15 días (≥ 11) con índice oscilante (aportado neto 100 constante) → std > 0.
            for (var i = 0; i < 15; i++)
            {
                var cap = 100m + (i % 2 == 0 ? 0m : 5m);   // alterna 100 / 105
                ctx.PortfolioSnapshots.Add(PortfolioSnapshot.Create(BaseTime.AddDays(i), cap, 0m, cap - 100m, 0, 0));
            }
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var m = await NewService(ctx2).GetPortfolioMetricsAsync();

        Assert.True(m.HasEnoughData);          // 14 retornos ≥ 10
        Assert.Equal(15, m.DailyPoints);
        Assert.True(m.AnnualizedVolatilityPct > 0);
        Assert.Null(m.Message);
    }

    [Fact]
    public async Task GetPortfolioMetrics_ProfitFactorDeTradesCerrados()
    {
        await using (var ctx = NewContext())
        {
            var win = Trade.Open("A", 100m, 10m, BaseTime); win.Close(110m, BaseTime.AddHours(1));   // +100
            var loss = Trade.Open("B", 100m, 10m, BaseTime); loss.Close(95m, BaseTime.AddHours(1));  // −50
            ctx.Trades.AddRange(win, loss);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var m = await NewService(ctx2).GetPortfolioMetricsAsync();

        Assert.Equal(2m, m.ProfitFactor);          // 100 / 50
        Assert.False(m.ProfitFactorInfinite);
        Assert.Equal(2, m.ClosedTrades);
    }

    [Fact]
    public async Task GetPortfolioMetrics_SinSnapshots_NoSuficiente()
    {
        await using var ctx = NewContext();
        var m = await NewService(ctx).GetPortfolioMetricsAsync();

        Assert.False(m.HasEnoughData);
        Assert.Equal(0, m.SnapshotCount);
        Assert.Equal(0m, m.MaxDrawdownPct);
        Assert.NotNull(m.Message);
    }

    [Fact]
    public async Task GetClosedTradesBreakdown_AgrupaPorAnioYMesYSumaPnL()
    {
        await using (var ctx = NewContext())
        {
            // 2026-05: +100 y −50 · 2026-06: +30 · 2025-12: +20. Sin TrackedSymbols → base EUR (rate 1).
            var a = Trade.Open("A", 100m, 10m, new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc));
            a.Close(110m, new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc));   // +100
            var b = Trade.Open("B", 100m, 10m, new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc));
            b.Close(95m, new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc));    // −50
            var c = Trade.Open("C", 50m, 10m, new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc));
            c.Close(53m, new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc));    // +30
            var d = Trade.Open("D", 10m, 2m, new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));
            d.Close(20m, new DateTime(2025, 12, 1, 12, 0, 0, DateTimeKind.Utc));    // +20
            ctx.Trades.AddRange(a, b, c, d);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var bd = await NewService(ctx2).GetClosedTradesBreakdownAsync();

        Assert.Equal(4, bd.TotalTrades);
        Assert.Equal(100m, bd.TotalPnLBase);          // 100 − 50 + 30 + 20
        Assert.Equal(2, bd.Years.Count);
        Assert.Equal(2026, bd.Years[0].Year);         // más reciente primero
        Assert.Equal(2025, bd.Years[1].Year);

        var y26 = bd.Years[0];
        Assert.Equal(80m, y26.PnLBase);               // 100 − 50 + 30
        Assert.Equal(3, y26.Trades);
        Assert.Equal(2, y26.Wins);
        Assert.Equal(1, y26.Losses);
        Assert.Equal(5, y26.Months[0].Month);         // meses ascendente: mayo, junio
        Assert.Equal(50m, y26.Months[0].PnLBase);     // 100 − 50
        Assert.Equal(6, y26.Months[1].Month);
        Assert.Equal(30m, y26.Months[1].PnLBase);
    }

    [Fact]
    public async Task GetClosedTradesBreakdown_SinTradesCerrados_Vacio()
    {
        await using var ctx = NewContext();
        var bd = await NewService(ctx).GetClosedTradesBreakdownAsync();

        Assert.Equal(0, bd.TotalTrades);
        Assert.Equal(0m, bd.TotalPnLBase);
        Assert.Empty(bd.Years);
    }

    [Fact]
    public async Task GetSnapshot_ComisionesYDividendos_AjustanElPnL()
    {
        await using (var ctx = NewContext())
        {
            var t = Trade.Open("A", 100m, 10m, BaseTime);
            t.Close(110m, BaseTime.AddHours(1));   // bruto +100
            t.AddCommission(2m);                   // compra + venta
            ctx.Trades.Add(t);
            ctx.Dividends.Add(Dividend.Create("A", 30m, BaseTime, "EUR"));   // dividendo +30
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var snap = await NewService(ctx2).GetSnapshotAsync();

        Assert.Equal(128m, snap.RealizedPnL);    // 100 − 2 + 30
        Assert.Equal(0m, snap.UnrealizedPnL);
        Assert.Equal(128m, snap.TotalPnL);
        Assert.Equal(128m, snap.Cash);           // 0 aportado − 0 invertido − 0 comisiones abiertas + 128
        Assert.Equal(128m, snap.AccountValue);   // invariante: cash + valor cartera (0)
    }

    [Fact]
    public async Task GetClosedTradesBreakdown_IncluyeDividendosPorPeriodo()
    {
        await using (var ctx = NewContext())
        {
            var t = Trade.Open("A", 100m, 10m, new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc));
            t.Close(110m, new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc));   // +100 en mayo 2026
            ctx.Trades.Add(t);
            ctx.Dividends.Add(Dividend.Create("A", 30m, new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), "EUR"));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var bd = await NewService(ctx2).GetClosedTradesBreakdownAsync();

        Assert.Equal(130m, bd.TotalPnLBase);       // 100 (trade) + 30 (dividendo)
        Assert.Equal(30m, bd.TotalDividendsBase);
        var may = bd.Years[0].Months[0];
        Assert.Equal(5, may.Month);
        Assert.Equal(130m, may.PnLBase);
        Assert.Equal(30m, may.DividendsBase);
        Assert.Equal(1, may.Trades);               // el dividendo no cuenta como trade
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>Stub de IFxRateProvider: rate 1 salvo los pares configurados.</summary>
    private sealed class StubFxRateProvider(Dictionary<(string, string), decimal>? rates = null) : IFxRateProvider
    {
        private readonly Dictionary<(string, string), decimal> _rates = rates ?? [];

        public Task<decimal> GetRateAsync(string from, string to, CancellationToken cancellationToken = default)
        {
            var f = (from ?? "").ToUpperInvariant();
            var t = (to ?? "").ToUpperInvariant();
            if (f.Length == 0 || t.Length == 0 || f == t) return Task.FromResult(1m);
            return Task.FromResult(_rates.GetValueOrDefault((f, t), 1m));
        }
    }

    /// <summary>Stub de IMarketProviderState (proveedor fijo) para tests.</summary>
    private sealed class StaticProviderState(string current = "YahooFinance") : IMarketProviderState
    {
        public string Current { get; } = current;
        public IReadOnlyList<string> Available { get; } = ["YahooFinance", "TwelveData"];
        public void Set(string provider) { }
    }
}

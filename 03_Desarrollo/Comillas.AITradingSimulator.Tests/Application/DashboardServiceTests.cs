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
    private readonly IOptionsMonitor<MarketDataOptions> _marketDataOpts;

    public DashboardServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new TradingDbContext(_options);
        ctx.Database.EnsureCreated();

        _marketDataOpts = new StaticOptionsMonitor<MarketDataOptions>(new MarketDataOptions { ProviderType = "YahooFinance" });
    }

    private TradingDbContext NewContext() => new(_options);
    private DashboardService NewService(TradingDbContext ctx) => new(ctx, _marketDataOpts);

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

    public void Dispose() => _connection.Dispose();

    /// <summary>Stub mínimo de IOptionsMonitor para tests.</summary>
    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T value) => CurrentValue = value;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}

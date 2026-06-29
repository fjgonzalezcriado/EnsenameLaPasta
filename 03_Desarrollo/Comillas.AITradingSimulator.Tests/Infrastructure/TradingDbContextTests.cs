using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Domain.Enums;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public sealed class TradingDbContextTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public TradingDbContextTests()
    {
        // SQLite :memory: con conexión mantenida abierta para que el schema persista
        // durante toda la vida del test (cerrar la conexión destruye la BD en :memory:).
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new TradingDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private TradingDbContext NewContext() => new(_options);

    [Fact]
    public async Task SaveAndRetrieve_Trade_RoundTripCorrecto()
    {
        var trade = Trade.Open("AAPL", 150.5m, 10m, BaseTime);
        trade.Close(160m, BaseTime.AddHours(2));
        var tradeId = trade.Id;

        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(trade);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var loaded = await ctx.Trades.FindAsync(tradeId);

            Assert.NotNull(loaded);
            Assert.Equal("AAPL", loaded!.Symbol);
            Assert.Equal(150.5m, loaded.EntryPrice);
            Assert.Equal(160m, loaded.ExitPrice);
            Assert.Equal(10m, loaded.Quantity);
            Assert.Equal(TradeStatus.Closed, loaded.Status);
            Assert.Equal(95m, loaded.RealizedPnL);  // (160 - 150.5) * 10 = 95
        }
    }

    [Fact]
    public async Task SaveAndRetrieve_MarketTick_RoundTripCorrecto()
    {
        var tick = MarketTick.Create("BTCUSD", 65_432.12345678m, 0.5m, BaseTime);
        var tickId = tick.Id;

        await using (var ctx = NewContext())
        {
            ctx.MarketTicks.Add(tick);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var loaded = await ctx.MarketTicks.FindAsync(tickId);

            Assert.NotNull(loaded);
            Assert.Equal("BTCUSD", loaded!.Symbol);
            Assert.Equal(65_432.12345678m, loaded.Price);
            Assert.Equal(0.5m, loaded.Volume);
            Assert.Equal(BaseTime, loaded.Timestamp);
        }
    }

    [Fact]
    public async Task SaveAndRetrieve_PortfolioSnapshot_RoundTripCorrecto()
    {
        var snapshot = PortfolioSnapshot.Create(BaseTime, 10_000m, 250.75m, -50m, 2, 8);
        var snapshotId = snapshot.Id;

        await using (var ctx = NewContext())
        {
            ctx.PortfolioSnapshots.Add(snapshot);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var loaded = await ctx.PortfolioSnapshots.FindAsync(snapshotId);

            Assert.NotNull(loaded);
            Assert.Equal(10_000m, loaded!.Capital);
            Assert.Equal(250.75m, loaded.RealizedPnL);
            Assert.Equal(-50m, loaded.UnrealizedPnL);
            Assert.Equal(200.75m, loaded.TotalPnL);
            Assert.Equal(2, loaded.OpenPositions);
            Assert.Equal(8, loaded.ClosedTrades);
        }
    }

    [Fact]
    public async Task DecimalPrecision_NoSePierdeEnRoundTrip()
    {
        // Caso típico que double/float corrompe: precio cripto con 8 decimales
        var preciosaltaPrecision = 0.00000123m;
        var tick = MarketTick.Create("SHIBUSD", preciosaltaPrecision, 1_000_000m, BaseTime);

        await using (var ctx = NewContext())
        {
            ctx.MarketTicks.Add(tick);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var loaded = await ctx.MarketTicks.FindAsync(tick.Id);
            Assert.NotNull(loaded);
            Assert.Equal(preciosaltaPrecision, loaded!.Price);  // EXACTO
        }
    }

    [Fact]
    public async Task TradeAbierto_RealizedPnLEsNull_TrasReload()
    {
        var trade = Trade.Open("AAPL", 100m, 5m, BaseTime);
        // No se cierra

        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(trade);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var loaded = await ctx.Trades.FindAsync(trade.Id);
            Assert.NotNull(loaded);
            Assert.Equal(TradeStatus.Open, loaded!.Status);
            Assert.Null(loaded.ExitPrice);
            Assert.Null(loaded.ClosedAt);
            Assert.Null(loaded.RealizedPnL);
        }
    }

    [Fact]
    public async Task Index_PorSymbolYTimestamp_PermiteFiltradoEficiente()
    {
        await using (var ctx = NewContext())
        {
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 150m, 100m, BaseTime));
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 151m, 200m, BaseTime.AddMinutes(1)));
            ctx.MarketTicks.Add(MarketTick.Create("GOOG", 2800m, 50m, BaseTime));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var aaplTicks = await ctx.MarketTicks
                .Where(t => t.Symbol == "AAPL")
                .OrderBy(t => t.Timestamp)
                .ToListAsync();

            Assert.Equal(2, aaplTicks.Count);
            Assert.Equal(150m, aaplTicks[0].Price);
            Assert.Equal(151m, aaplTicks[1].Price);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}

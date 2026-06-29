using Comillas.AITradingSimulator.Application.Services;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Domain.Enums;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Comillas.AITradingSimulator.Tests.Application;

public sealed class OrderServiceTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public OrderServiceTests()
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

    [Fact]
    public async Task BuyAsync_SinPosicionAbierta_PersisteNuevoTrade()
    {
        await using var ctx = NewContext();
        var svc = new OrderService(ctx);

        var trade = await svc.BuyAsync("AAPL", 150m, 10m, BaseTime);

        Assert.NotNull(trade);
        Assert.Equal("AAPL", trade!.Symbol);
        Assert.Equal(150m, trade.EntryPrice);
        Assert.Equal(TradeStatus.Open, trade.Status);

        var stored = await ctx.Trades.FindAsync(trade.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task BuyAsync_ConPosicionAbiertaParaMismoSymbol_RetornaNull()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 5m, BaseTime));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = new OrderService(ctx2);

        var trade = await svc.BuyAsync("AAPL", 150m, 10m, BaseTime.AddHours(1));

        Assert.Null(trade);
        Assert.Equal(1, await ctx2.Trades.CountAsync());  // sigue habiendo solo el original
    }

    [Fact]
    public async Task BuyAsync_ConPosicionAbiertaEnOtroSymbol_AbreNuevoTrade()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 5m, BaseTime));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = new OrderService(ctx2);

        // Compra distinta símbolo (GOOG, no AAPL)
        var trade = await svc.BuyAsync("GOOG", 2800m, 1m, BaseTime.AddHours(1));

        Assert.NotNull(trade);
        Assert.Equal(2, await ctx2.Trades.CountAsync());
    }

    [Fact]
    public async Task CloseAsync_TradeAbiertoExiste_CierraTradeYCalculaPnL()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 10m, BaseTime));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = new OrderService(ctx2);

        var closed = await svc.CloseAsync("AAPL", 115m, BaseTime.AddHours(1));

        Assert.NotNull(closed);
        Assert.Equal(TradeStatus.Closed, closed!.Status);
        Assert.Equal(115m, closed.ExitPrice);
        Assert.Equal(150m, closed.RealizedPnL);  // (115-100)*10 = 150
    }

    [Fact]
    public async Task CloseAsync_SinTradeAbierto_RetornaNull()
    {
        await using var ctx = NewContext();
        var svc = new OrderService(ctx);

        var closed = await svc.CloseAsync("AAPL", 100m, BaseTime);

        Assert.Null(closed);
    }

    [Fact]
    public async Task CloseAsync_SoloAfectaTradesAbiertos_NoTradesYaCerrados()
    {
        // Caso: tengo un Trade cerrado AAPL en BD. CloseAsync no debe encontrarlo ni intentar cerrarlo de nuevo.
        await using (var ctx = NewContext())
        {
            var t = Trade.Open("AAPL", 100m, 5m, BaseTime);
            t.Close(110m, BaseTime.AddHours(1));
            ctx.Trades.Add(t);
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = NewContext();
        var svc = new OrderService(ctx2);

        var result = await svc.CloseAsync("AAPL", 120m, BaseTime.AddHours(2));

        Assert.Null(result);
    }

    public void Dispose() => _connection.Dispose();
}

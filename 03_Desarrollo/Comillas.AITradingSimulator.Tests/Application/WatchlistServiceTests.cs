using Comillas.AITradingSimulator.Application.Services;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Comillas.AITradingSimulator.Tests.Application;

public sealed class WatchlistServiceTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public WatchlistServiceTests()
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
    private static WatchlistService NewService(TradingDbContext ctx) => new(ctx, TimeProvider.System);

    [Fact]
    public async Task AddAsync_SimboloNuevo_DevuelveTrueYPersiste()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        var added = await sut.AddAsync("HY9H.F", "SK hynix Inc.");

        Assert.True(added);
        var all = await sut.GetAllAsync();
        var item = Assert.Single(all);
        Assert.Equal("HY9H.F", item.Symbol);
        Assert.Equal("SK hynix Inc.", item.Name);
    }

    [Fact]
    public async Task AddAsync_Duplicado_DevuelveFalseSinDuplicar()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);
        await sut.AddAsync("HY9H.F", "SK hynix Inc.");

        // Mismo símbolo en minúsculas: debe detectarse como duplicado (normaliza a mayúsculas).
        var addedAgain = await sut.AddAsync("hy9h.f", "Otro nombre");

        Assert.False(addedAgain);
        Assert.Single(await sut.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_NormalizaAMayusculas()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        await sut.AddAsync("  aapl  ", "Apple");

        var item = Assert.Single(await sut.GetAllAsync());
        Assert.Equal("AAPL", item.Symbol);
    }

    [Fact]
    public async Task AddAsync_SimboloVacio_LanzaArgumentException()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.AddAsync("   ", "x"));
    }

    [Fact]
    public async Task RemoveAsync_Existente_DevuelveTrueYBorraTicks()
    {
        await using (var seed = NewContext())
        {
            seed.TrackedSymbols.Add(TrackedSymbol.Create("HY9H.F", "SK hynix Inc.", BaseTime));
            seed.MarketTicks.Add(MarketTick.Create("HY9H.F", 1390m, 10m, BaseTime));
            seed.MarketTicks.Add(MarketTick.Create("AAPL", 200m, 10m, BaseTime));
            await seed.SaveChangesAsync();
        }

        await using var ctx = NewContext();
        var sut = NewService(ctx);

        var removed = await sut.RemoveAsync("HY9H.F");

        Assert.True(removed);
        Assert.Empty(await sut.GetAllAsync());
        // Sus ticks se borran; los de otros símbolos permanecen.
        await using var verify = NewContext();
        Assert.Equal(0, await verify.MarketTicks.CountAsync(t => t.Symbol == "HY9H.F"));
        Assert.Equal(1, await verify.MarketTicks.CountAsync(t => t.Symbol == "AAPL"));
    }

    [Fact]
    public async Task RemoveAsync_NoExistente_DevuelveFalse()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        Assert.False(await sut.RemoveAsync("NOPE.F"));
    }

    [Fact]
    public async Task GetAllAsync_DevuelveOrdenadoPorSimbolo()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);
        await sut.AddAsync("ZZZ.F", "Z");
        await sut.AddAsync("AAA.F", "A");

        var all = await sut.GetAllAsync();

        Assert.Equal(["AAA.F", "ZZZ.F"], all.Select(t => t.Symbol));
    }

    public void Dispose() => _connection.Dispose();
}

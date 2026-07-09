using EnsenameLaPasta.Application.Services;
using EnsenameLaPasta.Domain.Entities;
using EnsenameLaPasta.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EnsenameLaPasta.Tests.Application;

public sealed class CashServiceTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2026, 6, 16, 8, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public CashServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TradingDbContext>().UseSqlite(_connection).Options;
        using var ctx = new TradingDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private TradingDbContext NewContext() => new(_options);
    private static CashService NewService(TradingDbContext ctx) => new(ctx, TimeProvider.System);

    [Fact]
    public async Task AddAsync_IngresoYRetirada_NetDepositsEsLaSumaNeta()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        await sut.AddAsync(10000m, "transferencia inicial", BaseTime);
        await sut.AddAsync(-1500m, "retirada", BaseTime.AddDays(1));

        Assert.Equal(8500m, await sut.GetNetDepositsAsync());
    }

    [Fact]
    public async Task AddAsync_ImporteCero_Lanza()
    {
        await using var ctx = NewContext();
        await Assert.ThrowsAsync<ArgumentException>(() => NewService(ctx).AddAsync(0m, "x", BaseTime));
    }

    [Fact]
    public async Task GetMovementsAsync_OrdenadoPorFechaDescendente()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);
        await sut.AddAsync(100m, "viejo", BaseTime);
        await sut.AddAsync(200m, "nuevo", BaseTime.AddHours(1));

        var movs = await sut.GetMovementsAsync();

        Assert.Equal(2, movs.Count);
        Assert.Equal("nuevo", movs[0].Note);   // más reciente primero
        Assert.Equal("viejo", movs[1].Note);
    }

    [Fact]
    public async Task DeleteAsync_Existente_DevuelveTrue_NoExistente_False()
    {
        Guid id;
        await using (var ctx = NewContext())
        {
            id = await NewService(ctx).AddAsync(500m, "dep", BaseTime);
        }

        await using var ctx2 = NewContext();
        Assert.True(await NewService(ctx2).DeleteAsync(id));

        await using var ctx3 = NewContext();
        Assert.False(await NewService(ctx3).DeleteAsync(id));
        Assert.Equal(0m, await NewService(ctx3).GetNetDepositsAsync());
    }

    [Fact]
    public async Task AddAsync_GuardaLaDivisaNormalizada_YGetMovementsLaDevuelve()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        await sut.AddAsync(1000m, "aporte usd", BaseTime, "usd");          // se normaliza a USD
        await sut.AddAsync(500m, "aporte sin divisa", BaseTime.AddHours(1)); // default EUR

        var movs = await sut.GetMovementsAsync();

        Assert.Equal("EUR", movs[0].Currency);   // más reciente (default)
        Assert.Equal("USD", movs[1].Currency);
    }

    public void Dispose() => _connection.Dispose();
}

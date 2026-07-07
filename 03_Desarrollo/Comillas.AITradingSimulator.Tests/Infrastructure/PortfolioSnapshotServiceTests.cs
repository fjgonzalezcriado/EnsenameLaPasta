using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Application.Services;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public sealed class PortfolioSnapshotServiceTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public PortfolioSnapshotServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new TradingDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    [Fact]
    public async Task TakeSnapshot_PersisteElValorDeCuentaActual()
    {
        // Arrange: aporta 10000 €, abre AAPL 100×5 (=500 invertido), último precio 110 (→ valor 550, PnL 50).
        using (var ctx = new TradingDbContext(_options))
        {
            ctx.CashMovements.Add(CashMovement.Create(10_000m, "inicial", BaseTime));
            ctx.Trades.Add(Trade.Open("AAPL", 100m, 5m, BaseTime));
            ctx.MarketTicks.Add(MarketTick.Create("AAPL", 110m, 100m, BaseTime.AddMinutes(1)));
            await ctx.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddDbContext<TradingDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<ITradingDbContext>(sp => sp.GetRequiredService<TradingDbContext>());
        services.AddSingleton<IMarketProviderState>(new StaticProviderState());
        // FX identidad (rate 1) para el snapshot.
        services.AddSingleton<IFxRateProvider, IdentityFxRateProvider>();
        services.AddSingleton<IOptions<FxOptions>>(Options.Create(new FxOptions { BaseCurrency = "EUR" }));
        services.AddScoped<IDashboardService, DashboardService>();
        await using var provider = services.BuildServiceProvider();

        var svc = new PortfolioSnapshotService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new StaticOptionsMonitor<SnapshotOptions>(new SnapshotOptions()),
            TimeProvider.System,
            NullLogger<PortfolioSnapshotService>.Instance);

        // Act
        await svc.TakeSnapshotAsync(CancellationToken.None);

        // Assert
        await using var verify = new TradingDbContext(_options);
        var snaps = await verify.PortfolioSnapshots.ToListAsync();

        Assert.Single(snaps);
        Assert.Equal(10_050m, snaps[0].Capital);        // 9500 efectivo + 550 cartera
        Assert.Equal(50m, snaps[0].UnrealizedPnL);
        Assert.Equal(0m, snaps[0].RealizedPnL);
        Assert.Equal(1, snaps[0].OpenPositions);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class IdentityFxRateProvider : IFxRateProvider
    {
        public Task<decimal> GetRateAsync(string from, string to, CancellationToken cancellationToken = default)
            => Task.FromResult(1m);
    }

    private sealed class StaticProviderState : IMarketProviderState
    {
        public string Current => "YahooFinance";
        public IReadOnlyList<string> Available { get; } = ["YahooFinance", "TwelveData"];
        public void Set(string provider) { }
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}

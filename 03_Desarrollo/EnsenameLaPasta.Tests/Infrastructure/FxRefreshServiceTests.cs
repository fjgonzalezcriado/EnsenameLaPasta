using System.Collections.Concurrent;
using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Application.Common.Options;
using EnsenameLaPasta.Domain.Entities;
using EnsenameLaPasta.Infrastructure.MarketData;
using EnsenameLaPasta.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Tests.Infrastructure;

public sealed class FxRefreshServiceTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public FxRefreshServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TradingDbContext>().UseSqlite(_connection).Options;
        using var ctx = new TradingDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    [Fact]
    public async Task RefreshInUseAsync_RefrescaDivisasEnUso_ExcluyendoBaseYDuplicados()
    {
        using (var ctx = new TradingDbContext(_options))
        {
            var aapl = TrackedSymbol.Create("AAPL", "Apple", BaseTime); aapl.SetCurrency("USD");
            var goog = TrackedSymbol.Create("GOOG", "Alphabet", BaseTime); goog.SetCurrency("USD"); // duplicada
            var hy = TrackedSymbol.Create("HY9H.F", "SK hynix", BaseTime); hy.SetCurrency("EUR");   // base → excluida
            ctx.TrackedSymbols.AddRange(aapl, goog, hy);
            ctx.CashMovements.Add(CashMovement.Create(1000m, "libras", BaseTime, "GBP"));
            await ctx.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddDbContext<TradingDbContext>(o => o.UseSqlite(_connection));
        await using var provider = services.BuildServiceProvider();

        var fx = new RecordingFxRateProvider();
        var svc = new FxRefreshService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            fx,
            new StaticOptionsMonitor(new FxOptions { BaseCurrency = "EUR" }),
            NullLogger<FxRefreshService>.Instance);

        await svc.RefreshInUseAsync(CancellationToken.None);

        Assert.Contains(("USD", "EUR"), fx.Refreshed);
        Assert.Contains(("GBP", "EUR"), fx.Refreshed);
        Assert.Equal(2, fx.Refreshed.Count);            // USD (dedupe) + GBP; EUR base excluida
        Assert.DoesNotContain(("EUR", "EUR"), fx.Refreshed);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class RecordingFxRateProvider : IFxRateProvider
    {
        public ConcurrentBag<(string From, string To)> Refreshed { get; } = [];
        public Task<decimal> GetRateAsync(string from, string to, CancellationToken cancellationToken = default)
            => Task.FromResult(1m);
        public Task RefreshAsync(string from, string to, CancellationToken cancellationToken = default)
        {
            Refreshed.Add((from, to));
            return Task.CompletedTask;
        }
    }

    private sealed class StaticOptionsMonitor(FxOptions value) : IOptionsMonitor<FxOptions>
    {
        public FxOptions CurrentValue { get; } = value;
        public FxOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<FxOptions, string?> listener) => null;
    }
}

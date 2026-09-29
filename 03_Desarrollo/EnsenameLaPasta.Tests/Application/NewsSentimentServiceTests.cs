using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Application.Services;
using EnsenameLaPasta.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EnsenameLaPasta.Tests.Application;

public sealed class NewsSentimentServiceTests : IDisposable
{
    private static readonly DateTime Day1 = new(2026, 9, 29, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day1Later = new(2026, 9, 29, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day2 = new(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public NewsSentimentServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TradingDbContext>().UseSqlite(_connection).Options;
        using var ctx = new TradingDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private TradingDbContext NewContext() => new(_options);

    private static NewsSentimentService NewService(TradingDbContext ctx, INewsProvider newsProvider, DateTime nowUtc)
        => new(ctx, newsProvider, new KeywordSentimentScorer(), new FixedTimeProvider(nowUtc));

    [Fact]
    public async Task CaptureTodayAsync_ConNoticias_PersisteElAgregadoDelDia()
    {
        await using var ctx = NewContext();
        var news = FakeNewsProvider.With("AAPL",
            "Apple stock surges on record profit",     // positiva (1.0)
            "Company shares plunge after losses");      // negativa (-1.0)
        var sut = NewService(ctx, news, Day1);

        var result = await sut.CaptureTodayAsync("AAPL");

        Assert.NotNull(result);
        Assert.Equal("AAPL", result!.Symbol);
        Assert.Equal(Day1.Date, result.DateUtc);
        Assert.Equal(0m, result.Score);   // media de +1 y -1
        Assert.Equal(2, result.SampleCount);

        await using var verify = NewContext();
        var row = Assert.Single(verify.NewsSentiments);
        Assert.Equal("AAPL", row.Symbol);
    }

    [Fact]
    public async Task CaptureTodayAsync_SinNoticias_DevuelveNullSinPersistir()
    {
        await using var ctx = NewContext();
        var news = FakeNewsProvider.SinCobertura("NUCL.SW");
        var sut = NewService(ctx, news, Day1);

        var result = await sut.CaptureTodayAsync("NUCL.SW");

        Assert.Null(result);
        await using var verify = NewContext();
        Assert.Empty(verify.NewsSentiments);
    }

    [Fact]
    public async Task CaptureTodayAsync_MismoDiaDosVeces_ActualizaLaMismaFilaSinDuplicar()
    {
        var newsPrimero = FakeNewsProvider.With("AAPL", "Apple surges on strong demand");   // +1
        await using (var ctx = NewContext())
        {
            var sut1 = NewService(ctx, newsPrimero, Day1);
            await sut1.CaptureTodayAsync("AAPL");
        }

        var newsSegundo = FakeNewsProvider.With("AAPL", "Apple plunges on weak guidance");  // -1
        await using (var ctx = NewContext())
        {
            // Misma jornada UTC, unas horas más tarde.
            var sut2 = NewService(ctx, newsSegundo, Day1Later);
            var result = await sut2.CaptureTodayAsync("AAPL");

            Assert.Equal(-1m, result!.Score);   // el segundo agregado sustituye al primero
        }

        await using var verify = NewContext();
        var row = Assert.Single(verify.NewsSentiments);   // sigue habiendo una sola fila (upsert)
        Assert.Equal(-1m, row.Score);
    }

    [Fact]
    public async Task CaptureTodayAsync_DiaDistinto_CreaUnaFilaNuevaSinTocarLaAnterior()
    {
        var newsDia1 = FakeNewsProvider.With("AAPL", "Apple surges on strong demand");
        await using (var ctx = NewContext())
        {
            var sut1 = NewService(ctx, newsDia1, Day1);
            await sut1.CaptureTodayAsync("AAPL");
        }

        var newsDia2 = FakeNewsProvider.With("AAPL", "Apple plunges on weak guidance");
        await using (var ctx = NewContext())
        {
            var sut2 = NewService(ctx, newsDia2, Day2);
            await sut2.CaptureTodayAsync("AAPL");
        }

        await using var verify = NewContext();
        Assert.Equal(2, await verify.NewsSentiments.CountAsync());
    }

    [Fact]
    public async Task CaptureTodayAsync_SimboloVacio_LanzaArgumentException()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx, FakeNewsProvider.With("X", "algo"), Day1);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CaptureTodayAsync("   "));
    }

    [Fact]
    public async Task GetHistoryAsync_DevuelveOrdenadoPorFechaAscendente()
    {
        await using (var ctx = NewContext())
        {
            var sut = NewService(ctx, FakeNewsProvider.With("AAPL", "Apple surges"), Day2);
            await sut.CaptureTodayAsync("AAPL");
        }
        await using (var ctx = NewContext())
        {
            var sut = NewService(ctx, FakeNewsProvider.With("AAPL", "Apple plunges"), Day1);
            await sut.CaptureTodayAsync("AAPL");
        }

        await using var readCtx = NewContext();
        var sutRead = NewService(readCtx, FakeNewsProvider.SinCobertura("AAPL"), Day2);
        var history = await sutRead.GetHistoryAsync("AAPL");

        Assert.Equal(2, history.Count);
        Assert.Equal(Day1.Date, history[0].DateUtc);
        Assert.Equal(Day2.Date, history[1].DateUtc);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class FakeNewsProvider(SymbolNews response) : INewsProvider
    {
        private readonly SymbolNews _response = response;

        public static FakeNewsProvider With(string symbol, params string[] titles)
        {
            var items = titles
                .Select(t => new NewsItem(t, "Fake", "https://example.test/" + Uri.EscapeDataString(t), null, []))
                .ToList();
            return new FakeNewsProvider(new SymbolNews(symbol, true, items, null));
        }

        public static FakeNewsProvider SinCobertura(string symbol)
            => new(new SymbolNews(symbol, false, [], "Sin noticias para este símbolo."));

        public Task<SymbolNews> GetNewsAsync(string symbol, int max = 8, CancellationToken cancellationToken = default)
            => Task.FromResult(_response);
    }
}

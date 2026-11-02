using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Application.Services;

namespace EnsenameLaPasta.Tests.Application;

public class NewsAggregatorServiceTests
{
    private static NewsItem YahooItem(string title, DateTime publishedAtUtc)
        => new(title, "Reuters", "https://yahoo.test/" + Uri.EscapeDataString(title), publishedAtUtc, [], "Yahoo");

    private static NewsItem OnvistaItem(string title, DateTime publishedAtUtc)
        => new(title, "onvista", "https://onvista.test/" + Uri.EscapeDataString(title), publishedAtUtc, [], "onvista");

    [Fact]
    public async Task GetNewsAsync_AmbasFuentesConDatos_CombinaYOrdenaPorFechaDesc()
    {
        var yahoo = new FakeNewsProvider(new SymbolNews("HY9H.F", true,
            [YahooItem("Yahoo antiguo", new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc))], null));
        var onvista = new FakeOnvistaNewsProvider(
            [OnvistaItem("onvista reciente", new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc))]);
        var sut = new NewsAggregatorService(yahoo, onvista);

        var result = await sut.GetNewsAsync("HY9H.F");

        Assert.True(result.HasNews);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("onvista reciente", result.Items[0].Title);   // más reciente primero
        Assert.Equal("Yahoo antiguo", result.Items[1].Title);
    }

    [Fact]
    public async Task GetNewsAsync_SoloYahooConDatos_DevuelveSoloEsas()
    {
        var yahoo = new FakeNewsProvider(new SymbolNews("AAPL", true,
            [YahooItem("Solo Yahoo", DateTime.UtcNow)], null));
        var onvista = new FakeOnvistaNewsProvider([]);   // sin mapeo para AAPL
        var sut = new NewsAggregatorService(yahoo, onvista);

        var result = await sut.GetNewsAsync("AAPL");

        Assert.True(result.HasNews);
        Assert.Single(result.Items);
        Assert.Equal("Solo Yahoo", result.Items[0].Title);
    }

    [Fact]
    public async Task GetNewsAsync_AmbasFuentesVacias_DevuelveHasNewsFalseConMensaje()
    {
        var yahoo = new FakeNewsProvider(new SymbolNews("NUCL.SW", false, [], "Sin noticias para este símbolo."));
        var onvista = new FakeOnvistaNewsProvider([]);
        var sut = new NewsAggregatorService(yahoo, onvista);

        var result = await sut.GetNewsAsync("NUCL.SW");

        Assert.False(result.HasNews);
        Assert.Empty(result.Items);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public async Task GetNewsAsync_RespetaElMaxTrasCombinar()
    {
        var yahoo = new FakeNewsProvider(new SymbolNews("HY9H.F", true,
            [
                YahooItem("Y1", new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc)),
                YahooItem("Y2", new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc))
            ], null));
        var onvista = new FakeOnvistaNewsProvider(
            [OnvistaItem("O1", new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc))]);
        var sut = new NewsAggregatorService(yahoo, onvista);

        var result = await sut.GetNewsAsync("HY9H.F", max: 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("O1", result.Items[0].Title);
        Assert.Equal("Y1", result.Items[1].Title);
    }

    [Fact]
    public async Task GetNewsAsync_SimboloVacio_DevuelveHasNewsFalseSinLlamarProviders()
    {
        var yahoo = new FakeNewsProvider(new SymbolNews("", true, [YahooItem("No debería verse", DateTime.UtcNow)], null));
        var onvista = new FakeOnvistaNewsProvider([OnvistaItem("No debería verse", DateTime.UtcNow)]);
        var sut = new NewsAggregatorService(yahoo, onvista);

        var result = await sut.GetNewsAsync("   ");

        Assert.False(result.HasNews);
        Assert.Empty(result.Items);
        Assert.False(yahoo.Called);
        Assert.False(onvista.Called);
    }

    private sealed class FakeNewsProvider(SymbolNews response) : INewsProvider
    {
        public bool Called { get; private set; }

        public Task<SymbolNews> GetNewsAsync(string symbol, int max = 8, CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.FromResult(response);
        }
    }

    private sealed class FakeOnvistaNewsProvider(IReadOnlyList<NewsItem> items) : IOnvistaNewsProvider
    {
        public bool Called { get; private set; }

        public Task<IReadOnlyList<NewsItem>> GetNewsAsync(string symbol, int max = 8, CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.FromResult(items);
        }
    }
}

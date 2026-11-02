using EnsenameLaPasta.Application.Common.Options;
using EnsenameLaPasta.Infrastructure.MarketData;
using EnsenameLaPasta.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Tests.Infrastructure;

// GetPreviousCloseAsync (HV-058): solo Yahoo soporta cierre anterior; con otro proveedor
// activo debe degradar a null sin tocar la red.
public sealed class SelectableMarketHistoryProviderTests : IDisposable
{
    private static readonly string FilePath = Path.Combine("App_Data", "active-provider.txt");
    private static readonly Uri YahooBaseUrl = new("https://query1.finance.yahoo.com");
    private static readonly Uri OtherBaseUrl = new("https://example.test");

    public SelectableMarketHistoryProviderTests() => CleanFile();
    public void Dispose() => CleanFile();

    private static void CleanFile()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { /* best-effort */ }
    }

    private static SelectableMarketHistoryProvider New(string activeProvider, MockHttpMessageHandler yahooHandler)
    {
        var yahoo = new YahooHistoryProvider(new TestHttpClientFactory(yahooHandler, YahooBaseUrl), NullLogger<YahooHistoryProvider>.Instance);
        var twelveData = new TwelveDataHistoryProvider(
            new TestHttpClientFactory(MockHttpMessageHandler.Json("{}"), OtherBaseUrl),
            new Opts<TwelveDataOptions>(new TwelveDataOptions()), TimeProvider.System, NullLogger<TwelveDataHistoryProvider>.Instance);
        var alphaVantage = new AlphaVantageHistoryProvider(
            new TestHttpClientFactory(MockHttpMessageHandler.Json("{}"), OtherBaseUrl),
            new Opts<AlphaVantageOptions>(new AlphaVantageOptions()), TimeProvider.System, NullLogger<AlphaVantageHistoryProvider>.Instance);
        var state = new MarketProviderState(
            Options.Create(new MarketDataOptions { ProviderType = activeProvider }),
            NullLogger<MarketProviderState>.Instance);

        return new SelectableMarketHistoryProvider(yahoo, twelveData, alphaVantage, state);
    }

    [Fact]
    public async Task GetPreviousCloseAsync_YahooActivo_DelegaEnYahoo()
    {
        const string json = """{"chart":{"result":[{"meta":{"previousClose":924.0}}]},"error":null}""";
        var handler = MockHttpMessageHandler.Json(json);
        var sut = New("YahooFinance", handler);

        var previousClose = await sut.GetPreviousCloseAsync("HY9H.F");

        Assert.Equal(924.0m, previousClose);
        Assert.Single(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetPreviousCloseAsync_TwelveDataActivo_DevuelveNullSinLlamarAYahoo()
    {
        var handler = MockHttpMessageHandler.Json("""{"chart":{"result":[{"meta":{"previousClose":924.0}}]}}""");
        var sut = New("TwelveData", handler);

        var previousClose = await sut.GetPreviousCloseAsync("AAPL");

        Assert.Null(previousClose);
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetPreviousCloseAsync_AlphaVantageActivo_DevuelveNullSinLlamarAYahoo()
    {
        var handler = MockHttpMessageHandler.Json("""{"chart":{"result":[{"meta":{"previousClose":924.0}}]}}""");
        var sut = New("AlphaVantage", handler);

        var previousClose = await sut.GetPreviousCloseAsync("AAPL");

        Assert.Null(previousClose);
        Assert.Empty(handler.ReceivedRequests);
    }

    private sealed class Opts<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}

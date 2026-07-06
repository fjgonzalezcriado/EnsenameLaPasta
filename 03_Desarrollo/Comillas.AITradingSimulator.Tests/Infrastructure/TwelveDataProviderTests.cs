using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class TwelveDataProviderTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 6, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Uri BaseUrl = new("https://api.twelvedata.com");

    private static TwelveDataProvider NewProvider(MockHttpMessageHandler handler, string apiKey = "demo-key")
    {
        var factory = new TestHttpClientFactory(handler, BaseUrl);
        var opts = new Opts(new TwelveDataOptions { ApiKey = apiKey });
        return new TwelveDataProvider(factory, opts, NullLogger<TwelveDataProvider>.Instance);
    }

    private const string ValidJson = """
        {"symbol":"AAPL","name":"Apple Inc","exchange":"NASDAQ","currency":"USD",
         "datetime":"2026-07-06","close":"173.45","volume":"12345678"}
        """;

    [Fact]
    public async Task GetLatestAsync_RespuestaValida_DevuelvePrecioVolumenYDivisa()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(ValidJson));

        var quote = await provider.GetLatestAsync("AAPL", BaseTime);

        Assert.Equal("AAPL", quote.Tick.Symbol);
        Assert.Equal(173.45m, quote.Tick.Price);
        Assert.Equal(12345678m, quote.Tick.Volume);
        Assert.Equal(BaseTime, quote.Tick.Timestamp);
        Assert.Equal("USD", quote.Currency);
    }

    [Fact]
    public async Task GetLatestAsync_SinApiKey_LanzaSinLlamarHttp()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("no debería llamar"));
        var provider = NewProvider(handler, apiKey: "");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetLatestAsync("AAPL", BaseTime));
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetLatestAsync_RespuestaError_LanzaInvalidOperation()
    {
        const string err = """{"code":401,"message":"Invalid API key","status":"error"}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(err));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetLatestAsync("AAPL", BaseTime));
    }

    [Fact]
    public async Task GetLatestAsync_Http404ConCuerpoError_LanzaMensajeClaro()
    {
        // Símbolo no disponible en el plan → Twelve Data responde 404 con JSON de error.
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                """{"code":404,"message":"This symbol is available starting with the Grow plan","status":"error"}""",
                System.Text.Encoding.UTF8, "application/json")
        });
        var provider = NewProvider(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetLatestAsync("HY9H", BaseTime));
        Assert.Contains("Grow plan", ex.Message);   // mensaje de TD, no "404 Not Found"
    }

    [Fact]
    public async Task GetLatestAsync_SinVolumen_UsaCero()
    {
        const string noVol = """{"close":"50.5","currency":"EUR"}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(noVol));

        var quote = await provider.GetLatestAsync("XYZ", BaseTime);

        Assert.Equal(50.5m, quote.Tick.Price);
        Assert.Equal(0m, quote.Tick.Volume);
        Assert.Equal("EUR", quote.Currency);
    }

    [Fact]
    public async Task GetLatestAsync_LlamaAlEndpointQuoteConSimboloYKey()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(ValidJson, System.Text.Encoding.UTF8, "application/json")
        });
        var provider = NewProvider(handler, apiKey: "K123");

        await provider.GetLatestAsync("AAPL", BaseTime);

        var req = Assert.Single(handler.ReceivedRequests);
        Assert.Contains("/quote", req.RequestUri!.AbsolutePath);
        Assert.Contains("symbol=AAPL", req.RequestUri.Query);
        Assert.Contains("apikey=K123", req.RequestUri.Query);
    }

    private sealed class Opts : IOptionsMonitor<TwelveDataOptions>
    {
        public Opts(TwelveDataOptions value) => CurrentValue = value;
        public TwelveDataOptions CurrentValue { get; }
        public TwelveDataOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<TwelveDataOptions, string?> listener) => null;
    }
}

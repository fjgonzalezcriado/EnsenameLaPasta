using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class AlphaVantageProviderTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Uri BaseUrl = new("https://www.alphavantage.co");

    private static AlphaVantageProvider NewProvider(MockHttpMessageHandler handler, string apiKey = "demo-key")
    {
        var factory = new TestHttpClientFactory(handler, BaseUrl);
        var opts = new Opts(new AlphaVantageOptions { ApiKey = apiKey });
        return new AlphaVantageProvider(factory, opts, NullLogger<AlphaVantageProvider>.Instance);
    }

    private const string ValidJson = """
        {"Global Quote":{"01. symbol":"IBM","05. price":"290.50","06. volume":"3200000"}}
        """;

    [Fact]
    public async Task GetLatestAsync_RespuestaValida_DevuelvePrecioYVolumen()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(ValidJson));

        var quote = await provider.GetLatestAsync("IBM", BaseTime);

        Assert.Equal("IBM", quote.Tick.Symbol);
        Assert.Equal(290.50m, quote.Tick.Price);
        Assert.Equal(3200000m, quote.Tick.Volume);
        Assert.Equal(BaseTime, quote.Tick.Timestamp);
        Assert.Equal(string.Empty, quote.Currency);   // GLOBAL_QUOTE no informa divisa
    }

    [Fact]
    public async Task GetLatestAsync_SinApiKey_LanzaSinLlamarHttp()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("no debería llamar"));
        var provider = NewProvider(handler, apiKey: "");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetLatestAsync("IBM", BaseTime));
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetLatestAsync_LimiteDeCuota_LanzaMensajeClaro()
    {
        // Alpha Vantage responde 200 con "Information" al agotar la cuota (plan gratuito 25/día).
        const string info = """{"Information":"We have detected your API key ... 25 requests per day ... premium plan"}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(info));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetLatestAsync("IBM", BaseTime));
        Assert.Contains("25 requests per day", ex.Message);
    }

    [Fact]
    public async Task GetLatestAsync_ErrorMessage_Lanza()
    {
        const string err = """{"Error Message":"Invalid API call ... invalid symbol"}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(err));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetLatestAsync("XXXX", BaseTime));
    }

    [Fact]
    public async Task GetLatestAsync_SinVolumen_UsaCero()
    {
        const string noVol = """{"Global Quote":{"05. price":"50.5"}}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(noVol));

        var quote = await provider.GetLatestAsync("XYZ", BaseTime);

        Assert.Equal(50.5m, quote.Tick.Price);
        Assert.Equal(0m, quote.Tick.Volume);
    }

    [Fact]
    public async Task GetLatestAsync_LlamaAGlobalQuoteConSimboloYKey()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(ValidJson, System.Text.Encoding.UTF8, "application/json")
        });
        var provider = NewProvider(handler, apiKey: "K123");

        await provider.GetLatestAsync("IBM", BaseTime);

        var req = Assert.Single(handler.ReceivedRequests);
        Assert.Contains("function=GLOBAL_QUOTE", req.RequestUri!.Query);
        Assert.Contains("symbol=IBM", req.RequestUri.Query);
        Assert.Contains("apikey=K123", req.RequestUri.Query);
    }

    private sealed class Opts : IOptionsMonitor<AlphaVantageOptions>
    {
        public Opts(AlphaVantageOptions value) => CurrentValue = value;
        public AlphaVantageOptions CurrentValue { get; }
        public AlphaVantageOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AlphaVantageOptions, string?> listener) => null;
    }
}

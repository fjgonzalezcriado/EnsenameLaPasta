using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class AlphaVantageInstrumentSearchProviderTests
{
    private static readonly Uri BaseUrl = new("https://www.alphavantage.co");

    private static AlphaVantageInstrumentSearchProvider NewProvider(MockHttpMessageHandler handler, string apiKey = "demo")
        => new(new TestHttpClientFactory(handler, BaseUrl),
               new Opts(new AlphaVantageOptions { ApiKey = apiKey }),
               NullLogger<AlphaVantageInstrumentSearchProvider>.Instance);

    private const string Json = """
        {"bestMatches":[
          {"1. symbol":"IBM","2. name":"International Business Machines","3. type":"Equity","4. region":"United States","8. currency":"USD"},
          {"1. symbol":"IBN","2. name":"ICICI Bank","3. type":"Equity","4. region":"United States","8. currency":"USD"}
        ]}
        """;

    [Fact]
    public async Task SearchAsync_MapeaResultados()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(Json));

        var results = await provider.SearchAsync("ibm");

        Assert.Equal(2, results.Count);
        Assert.Equal("IBM", results[0].Symbol);
        Assert.Equal("International Business Machines", results[0].Name);
        Assert.Equal("United States", results[0].Exchange);   // region → exchange
        Assert.Equal("Equity", results[0].Type);
    }

    [Fact]
    public async Task SearchAsync_SinApiKey_DevuelveVacioSinLlamar()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("no debería llamar"));
        var provider = NewProvider(handler, apiKey: "");

        Assert.Empty(await provider.SearchAsync("ibm"));
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task SearchAsync_NotaDeCuota_DevuelveVacio()
    {
        const string note = """{"Information":"... 25 requests per day ..."}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(note));

        Assert.Empty(await provider.SearchAsync("ibm"));
    }

    [Fact]
    public async Task SearchAsync_LlamaASymbolSearchConKeywordsYKey()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(Json, System.Text.Encoding.UTF8, "application/json")
        });
        var provider = NewProvider(handler, apiKey: "K9");

        await provider.SearchAsync("ibm");

        var req = Assert.Single(handler.ReceivedRequests);
        Assert.Contains("function=SYMBOL_SEARCH", req.RequestUri!.Query);
        Assert.Contains("keywords=ibm", req.RequestUri.Query);
        Assert.Contains("apikey=K9", req.RequestUri.Query);
    }

    private sealed class Opts(AlphaVantageOptions value) : IOptionsMonitor<AlphaVantageOptions>
    {
        public AlphaVantageOptions CurrentValue { get; } = value;
        public AlphaVantageOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AlphaVantageOptions, string?> listener) => null;
    }
}

using System.Net;
using System.Text.Json;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class YahooInstrumentSearchProviderTests
{
    private static readonly Uri BaseUrl = new("https://query1.finance.yahoo.com");

    private static YahooInstrumentSearchProvider NewProvider(MockHttpMessageHandler handler)
    {
        var factory = new TestHttpClientFactory(handler, BaseUrl);
        return new YahooInstrumentSearchProvider(factory, NullLogger<YahooInstrumentSearchProvider>.Instance);
    }

    // Forma del endpoint /v1/finance/search: los instrumentos viven en "quotes".
    private const string ValidJson = """
        {
            "quotes": [
                {
                    "symbol": "HY9H.F",
                    "shortname": "SK Hynix Inc.",
                    "longname": "SK hynix Inc.",
                    "exchange": "FRA",
                    "exchDisp": "Frankfurt",
                    "quoteType": "EQUITY",
                    "typeDisp": "Equity"
                },
                {
                    "symbol": "000660.KS",
                    "shortname": "SK hynix",
                    "longname": "SK hynix Inc.",
                    "exchange": "KSC",
                    "exchDisp": "Korea",
                    "quoteType": "EQUITY",
                    "typeDisp": "Equity"
                }
            ],
            "news": []
        }
        """;

    [Fact]
    public async Task SearchAsync_RespuestaValida_MapeaResultados()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(ValidJson));

        var results = await provider.SearchAsync("SK hynix");

        Assert.Equal(2, results.Count);
        var fra = results[0];
        Assert.Equal("HY9H.F", fra.Symbol);
        Assert.Equal("SK hynix Inc.", fra.Name);   // prioriza longname
        Assert.Equal("Frankfurt", fra.Exchange);   // prioriza exchDisp
        Assert.Equal("Equity", fra.Type);           // prioriza typeDisp
    }

    [Fact]
    public async Task SearchAsync_QueryVacia_DevuelveVacioSinLlamar()
    {
        var handler = MockHttpMessageHandler.Json(ValidJson);
        var provider = NewProvider(handler);

        var results = await provider.SearchAsync("   ");

        Assert.Empty(results);
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task SearchAsync_SinQuotes_DevuelveVacio()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json("""{"quotes":[],"news":[]}"""));

        var results = await provider.SearchAsync("nada-que-coincida");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_QuoteSinSymbol_SeFiltra()
    {
        const string json = """
            {"quotes":[{"shortname":"Sin símbolo","quoteType":"EQUITY"},{"symbol":"AAPL","longname":"Apple Inc.","exchDisp":"NASDAQ","typeDisp":"Equity"}],"news":[]}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(json));

        var results = await provider.SearchAsync("apple");

        Assert.Single(results);
        Assert.Equal("AAPL", results[0].Symbol);
    }

    [Fact]
    public async Task SearchAsync_SoloShortName_UsaShortNameComoNombre()
    {
        const string json = """
            {"quotes":[{"symbol":"X.F","shortname":"Solo corto","exchange":"FRA","quoteType":"INDEX"}],"news":[]}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(json));

        var results = await provider.SearchAsync("x");

        Assert.Equal("Solo corto", results[0].Name);
        Assert.Equal("FRA", results[0].Exchange);     // cae a exchange si no hay exchDisp
        Assert.Equal("INDEX", results[0].Type);        // cae a quoteType si no hay typeDisp
    }

    [Fact]
    public async Task SearchAsync_LlamaAlEndpointEsperado()
    {
        var handler = MockHttpMessageHandler.Json(ValidJson);
        var provider = NewProvider(handler);

        await provider.SearchAsync("KR7000660001");

        var request = Assert.Single(handler.ReceivedRequests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("/v1/finance/search", request.RequestUri!.AbsolutePath);
        Assert.Contains("q=KR7000660001", request.RequestUri.Query);
    }

    [Fact]
    public async Task SearchAsync_Http500_LanzaHttpRequestException()
    {
        var provider = NewProvider(MockHttpMessageHandler.Status(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.SearchAsync("apple"));
    }

    [Fact]
    public async Task SearchAsync_JsonMalformado_LanzaJsonException()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json("{ malformed"));

        await Assert.ThrowsAsync<JsonException>(() => provider.SearchAsync("apple"));
    }
}

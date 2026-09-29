using System.Net;
using EnsenameLaPasta.Infrastructure.MarketData;
using EnsenameLaPasta.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnsenameLaPasta.Tests.Infrastructure;

public class YahooNewsProviderTests
{
    private static readonly Uri BaseUrl = new("https://query1.finance.yahoo.com");

    private static YahooNewsProvider NewProvider(MockHttpMessageHandler handler)
    {
        var factory = new TestHttpClientFactory(handler, BaseUrl);
        return new YahooNewsProvider(factory, NullLogger<YahooNewsProvider>.Instance);
    }

    // Forma real del endpoint /v1/finance/search con newsCount > 0.
    private const string ValidJson = """
        {
            "quotes": [],
            "news": [
                {
                    "uuid": "a1",
                    "title": "Apple presenta resultados",
                    "publisher": "Reuters",
                    "link": "https://finance.yahoo.com/news/apple-a1",
                    "providerPublishTime": 1700000200,
                    "relatedTickers": ["AAPL", "MSFT"]
                },
                {
                    "uuid": "a2",
                    "title": "Análisis: el sector tecnológico",
                    "publisher": "GuruFocus",
                    "link": "https://finance.yahoo.com/news/a2",
                    "providerPublishTime": 1700000100,
                    "relatedTickers": ["AAPL"]
                }
            ]
        }
        """;

    [Fact]
    public async Task GetNewsAsync_RespuestaValida_MapeaYOrdenaPorFechaDesc()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(ValidJson));

        var result = await provider.GetNewsAsync("AAPL");

        Assert.True(result.HasNews);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Apple presenta resultados", result.Items[0].Title);   // publishTime mayor primero
        Assert.Equal("Reuters", result.Items[0].Publisher);
        Assert.Equal("https://finance.yahoo.com/news/apple-a1", result.Items[0].Link);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000200).UtcDateTime, result.Items[0].PublishedAtUtc);
        Assert.Contains("MSFT", result.Items[0].RelatedTickers);
    }

    [Fact]
    public async Task GetNewsAsync_SinNoticias_DevuelveHasNewsFalseConMensaje()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json("""{"quotes":[],"news":[]}"""));

        var result = await provider.GetNewsAsync("NUCL.SW");

        Assert.False(result.HasNews);
        Assert.Empty(result.Items);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public async Task GetNewsAsync_SimboloVacio_DevuelveHasNewsFalseSinLlamar()
    {
        var handler = MockHttpMessageHandler.Json(ValidJson);
        var provider = NewProvider(handler);

        var result = await provider.GetNewsAsync("   ");

        Assert.False(result.HasNews);
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetNewsAsync_Http500_DegradaSinLanzar()
    {
        var provider = NewProvider(MockHttpMessageHandler.Status(HttpStatusCode.InternalServerError));

        var result = await provider.GetNewsAsync("AAPL");

        Assert.False(result.HasNews);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetNewsAsync_JsonMalformado_DegradaSinLanzar()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json("{ malformed"));

        var result = await provider.GetNewsAsync("AAPL");

        Assert.False(result.HasNews);
    }

    [Fact]
    public async Task GetNewsAsync_ItemSinTituloOLink_SeFiltra()
    {
        const string json = """
            {"quotes":[],"news":[
                {"uuid":"b1","title":"Sin link","publisher":"X","providerPublishTime":1700000000},
                {"uuid":"b2","publisher":"Y","link":"https://x.com/b2","providerPublishTime":1700000000},
                {"uuid":"b3","title":"Válida","link":"https://x.com/b3","providerPublishTime":1700000000}
            ]}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(json));

        var result = await provider.GetNewsAsync("AAPL");

        Assert.True(result.HasNews);
        Assert.Single(result.Items);
        Assert.Equal("Válida", result.Items[0].Title);
    }

    [Fact]
    public async Task GetNewsAsync_RespetaElMaxIndicado()
    {
        const string json = """
            {"quotes":[],"news":[
                {"uuid":"c1","title":"Uno","link":"https://x.com/1","providerPublishTime":3},
                {"uuid":"c2","title":"Dos","link":"https://x.com/2","providerPublishTime":2},
                {"uuid":"c3","title":"Tres","link":"https://x.com/3","providerPublishTime":1}
            ]}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(json));

        var result = await provider.GetNewsAsync("AAPL", max: 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Uno", result.Items[0].Title);
        Assert.Equal("Dos", result.Items[1].Title);
    }

    [Fact]
    public async Task GetNewsAsync_LlamaAlEndpointEsperadoConNewsCount()
    {
        var handler = MockHttpMessageHandler.Json(ValidJson);
        var provider = NewProvider(handler);

        await provider.GetNewsAsync("AAPL", max: 6);

        var request = Assert.Single(handler.ReceivedRequests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("/v1/finance/search", request.RequestUri!.AbsolutePath);
        Assert.Contains("q=AAPL", request.RequestUri.Query);
        Assert.Contains("newsCount=6", request.RequestUri.Query);
        Assert.Contains("quotesCount=0", request.RequestUri.Query);
    }
}

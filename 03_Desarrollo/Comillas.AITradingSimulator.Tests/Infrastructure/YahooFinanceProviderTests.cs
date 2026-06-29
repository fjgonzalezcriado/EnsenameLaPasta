using System.Net;
using System.Text.Json;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class YahooFinanceProviderTests
{
    private static readonly DateTime BaseTime = new(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Uri BaseUrl = new("https://query1.finance.yahoo.com");

    private static YahooFinanceProvider NewProvider(MockHttpMessageHandler handler)
    {
        var factory = new TestHttpClientFactory(handler, BaseUrl);
        return new YahooFinanceProvider(factory, NullLogger<YahooFinanceProvider>.Instance);
    }

    // Forma del endpoint /v8/finance/chart: precio + volumen viven en chart.result[0].meta.
    private const string ValidJson = """
        {
            "chart": {
                "result": [
                    {
                        "meta": {
                            "symbol": "AAPL",
                            "regularMarketPrice": 173.45,
                            "regularMarketVolume": 12345678,
                            "currency": "USD"
                        }
                    }
                ],
                "error": null
            }
        }
        """;

    [Fact]
    public async Task GetLatestAsync_RespuestaValida_DevuelveQuoteConPrecioYDivisa()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(ValidJson));

        var quote = await provider.GetLatestAsync("AAPL", BaseTime);

        Assert.Equal("AAPL", quote.Tick.Symbol);
        Assert.Equal(173.45m, quote.Tick.Price);
        Assert.Equal(12345678m, quote.Tick.Volume);
        Assert.Equal(BaseTime, quote.Tick.Timestamp);
        Assert.Equal("USD", quote.Currency);   // meta.currency
    }

    [Fact]
    public async Task GetLatestAsync_SinCurrencyEnMeta_DivisaNull()
    {
        const string noCurrency = """
            {"chart":{"result":[{"meta":{"symbol":"AAPL","regularMarketPrice":150.5,"regularMarketVolume":100}}],"error":null}}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(noCurrency));

        var quote = await provider.GetLatestAsync("AAPL", BaseTime);

        Assert.Null(quote.Currency);
        Assert.Equal(150.5m, quote.Tick.Price);
    }

    [Fact]
    public async Task GetLatestAsync_RespuestaSinResult_LanzaInvalidOperation()
    {
        const string emptyJson = """{"chart":{"result":[],"error":null}}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(emptyJson));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetLatestAsync("AAPL", BaseTime));
    }

    [Fact]
    public async Task GetLatestAsync_ResultNull_LanzaInvalidOperation()
    {
        const string nullResultJson = """{"chart":{"result":null,"error":null}}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(nullResultJson));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetLatestAsync("AAPL", BaseTime));
    }

    [Fact]
    public async Task GetLatestAsync_PrecioCero_LanzaInvalidOperation()
    {
        const string zeroPrice = """
            {"chart":{"result":[{"meta":{"symbol":"AAPL","regularMarketPrice":0,"regularMarketVolume":100}}],"error":null}}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(zeroPrice));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetLatestAsync("AAPL", BaseTime));
    }

    [Fact]
    public async Task GetLatestAsync_PrecioNull_LanzaInvalidOperation()
    {
        const string nullPrice = """
            {"chart":{"result":[{"meta":{"symbol":"AAPL","regularMarketPrice":null,"regularMarketVolume":100}}],"error":null}}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(nullPrice));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetLatestAsync("AAPL", BaseTime));
    }

    [Fact]
    public async Task GetLatestAsync_Http404_LanzaHttpRequestException()
    {
        var provider = NewProvider(MockHttpMessageHandler.Status(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GetLatestAsync("AAPL", BaseTime));
    }

    [Fact]
    public async Task GetLatestAsync_Http500_LanzaHttpRequestException()
    {
        var provider = NewProvider(MockHttpMessageHandler.Status(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GetLatestAsync("AAPL", BaseTime));
    }

    [Fact]
    public async Task GetLatestAsync_JsonMalformado_LanzaJsonException()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json("{ malformed json"));

        await Assert.ThrowsAsync<JsonException>(
            () => provider.GetLatestAsync("AAPL", BaseTime));
    }

    [Fact]
    public async Task GetLatestAsync_VolumeNull_UsaCero()
    {
        const string nullVolume = """
            {"chart":{"result":[{"meta":{"symbol":"AAPL","regularMarketPrice":150.5,"regularMarketVolume":null}}],"error":null}}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(nullVolume));

        // MarketTick.Create exige volume >= 0; con null lo mapeamos a 0 (válido)
        var quote = await provider.GetLatestAsync("AAPL", BaseTime);
        Assert.Equal(0m, quote.Tick.Volume);
    }

    [Fact]
    public async Task GetLatestAsync_LlamaAlEndpointEsperado()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidJson, System.Text.Encoding.UTF8, "application/json")
        });
        var provider = NewProvider(handler);

        await provider.GetLatestAsync("AAPL", BaseTime);

        Assert.Single(handler.ReceivedRequests);
        var request = handler.ReceivedRequests[0];
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("/v8/finance/chart/AAPL", request.RequestUri!.AbsolutePath);
        Assert.Contains("interval=1d", request.RequestUri.Query);
    }

    [Fact]
    public async Task GetLatestAsync_SymbolConCaracteresEspeciales_EnElPath()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidJson, System.Text.Encoding.UTF8, "application/json")
        });
        var provider = NewProvider(handler);

        await provider.GetLatestAsync("BTC-USD", BaseTime);

        Assert.Single(handler.ReceivedRequests);
        var request = handler.ReceivedRequests[0];
        // El símbolo BTC-USD va en el path (el guion no necesita encoding).
        Assert.Contains("/v8/finance/chart/BTC-USD", request.RequestUri!.AbsolutePath);
    }
}

using EnsenameLaPasta.Infrastructure.MarketData;
using EnsenameLaPasta.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnsenameLaPasta.Tests.Infrastructure;

public class YahooHistoryProviderTests
{
    private static readonly Uri BaseUrl = new("https://query1.finance.yahoo.com");

    private static YahooHistoryProvider NewProvider(MockHttpMessageHandler handler)
        => new(new TestHttpClientFactory(handler, BaseUrl), NullLogger<YahooHistoryProvider>.Instance);

    // /v8/chart: cierre y volumen viven en indicators.quote[0], en paralelo a timestamp.
    private const string Json = """
        {
          "chart": { "result": [ {
            "timestamp": [1700000000, 1700000300, 1700000600],
            "indicators": { "quote": [ {
              "close":  [100.0, null, 102.0],
              "open":   [99.0, null, 101.5],
              "high":   [100.5, null, 103.0],
              "low":    [98.5, null, 101.0],
              "volume": [1000, 2000, 3000]
            } ] }
          } ], "error": null }
        }
        """;

    [Fact]
    public async Task GetHistoryAsync_ParseaPrecioYVolumen_YSaltaCierresNulos()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(Json));

        var points = await provider.GetHistoryAsync("AAPL", "1D");

        // La 2ª barra (close null) se descarta; quedan la 1ª y la 3ª con su volumen alineado.
        Assert.Equal(2, points.Count);
        Assert.Equal(100m, points[0].Price);
        Assert.Equal(1000m, points[0].Volume);
        Assert.Equal(102m, points[1].Price);
        Assert.Equal(3000m, points[1].Volume);
    }

    [Fact]
    public async Task GetHistoryAsync_SinArrayDeVolumen_VolumenCero()
    {
        const string noVol = """
            {"chart":{"result":[{"timestamp":[1700000000],"indicators":{"quote":[{"close":[50.0]}]}}],"error":null}}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(noVol));

        var points = await provider.GetHistoryAsync("AAPL", "1D");

        Assert.Single(points);
        Assert.Equal(50m, points[0].Price);
        Assert.Equal(0m, points[0].Volume);
        // Sin arrays OHLC → open/high/low caen al cierre (vela plana), no cero (HV-041).
        Assert.Equal(50m, points[0].Open);
        Assert.Equal(50m, points[0].High);
        Assert.Equal(50m, points[0].Low);
    }

    [Fact]
    public async Task GetHistoryAsync_ParseaOHLC_ParaVelas()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(Json));

        var points = await provider.GetHistoryAsync("AAPL", "1D");

        // 1ª barra: OHLC completo alineado con el cierre.
        Assert.Equal(99.0m, points[0].Open);
        Assert.Equal(100.5m, points[0].High);
        Assert.Equal(98.5m, points[0].Low);
        Assert.Equal(100.0m, points[0].Price);   // cierre
        // 2ª barra devuelta (3ª del array; la del medio se descartó por cierre nulo).
        Assert.Equal(101.5m, points[1].Open);
        Assert.Equal(103.0m, points[1].High);
        Assert.Equal(101.0m, points[1].Low);
        Assert.Equal(102.0m, points[1].Price);
    }
}

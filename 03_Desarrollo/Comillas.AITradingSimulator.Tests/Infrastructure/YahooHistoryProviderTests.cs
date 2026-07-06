using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

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
    }
}

using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class AlphaVantageHistoryProviderTests
{
    private static readonly Uri BaseUrl = new("https://www.alphavantage.co");

    private static AlphaVantageHistoryProvider NewProvider(MockHttpMessageHandler handler, string apiKey = "demo")
        => new(new TestHttpClientFactory(handler, BaseUrl),
               new Opts(new AlphaVantageOptions { ApiKey = apiKey }),
               TimeProvider.System,
               NullLogger<AlphaVantageHistoryProvider>.Instance);

    // La clave de la serie diaria es "Time Series (Daily)"; Alpha Vantage la da del más
    // reciente al más antiguo.
    private const string DailyJson = """
        {
          "Meta Data": { "2. Symbol": "IBM" },
          "Time Series (Daily)": {
            "2026-07-06": {"1. open":"101.5","2. high":"103.0","3. low":"101.0","4. close":"102.0","5. volume":"3000"},
            "2026-07-03": {"1. open":"100.5","2. high":"101.5","3. low":"100.0","4. close":"101.0","5. volume":"2000"},
            "2026-07-02": {"1. open":"99.0","2. high":"100.5","3. low":"98.5","4. close":"100.0","5. volume":"1000"}
          }
        }
        """;

    [Fact]
    public async Task GetHistoryAsync_OrdenaAscendente_YParseaOHLC()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(DailyJson));

        var points = await provider.GetHistoryAsync("IBM", "1M");

        Assert.Equal(3, points.Count);
        Assert.True(points[0].Timestamp < points[2].Timestamp);   // ascendente
        Assert.Equal(100.0m, points[0].Price);
        Assert.Equal(99.0m, points[0].Open);
        Assert.Equal(100.5m, points[0].High);
        Assert.Equal(98.5m, points[0].Low);
        Assert.Equal(1000m, points[0].Volume);
        Assert.Equal(102.0m, points[2].Price);
        Assert.Equal(103.0m, points[2].High);
    }

    [Fact]
    public async Task GetHistoryAsync_ParseaSerieIntradia()
    {
        // En 1D la clave es "Time Series (5min)".
        const string intraday = """
            {"Time Series (5min)":{
              "2026-07-07 15:55:00":{"1. open":"10.0","2. high":"10.5","3. low":"9.9","4. close":"10.2","5. volume":"500"}
            }}
            """;
        var provider = NewProvider(MockHttpMessageHandler.Json(intraday));

        var points = await provider.GetHistoryAsync("IBM", "1D");

        Assert.Single(points);
        Assert.Equal(10.2m, points[0].Price);
        Assert.Equal(10.5m, points[0].High);
    }

    [Fact]
    public async Task GetHistoryAsync_NotaDeCuota_DevuelveVacio()
    {
        const string note = """{"Note":"Thank you for using Alpha Vantage! ... 25 requests per day ..."}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(note));

        Assert.Empty(await provider.GetHistoryAsync("IBM", "1M"));
    }

    [Fact]
    public async Task GetHistoryAsync_SinApiKey_DevuelveVacioSinLlamar()
    {
        // Sin key el histórico degrada a vacío (no lanza) para no ensuciar el log con stacks.
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("no debería llamar"));
        var provider = NewProvider(handler, apiKey: "");

        Assert.Empty(await provider.GetHistoryAsync("IBM", "1M"));
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetHistoryAsync_RangoNoSoportado_Lanza()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(DailyJson));
        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetHistoryAsync("IBM", "10Y"));
    }

    private sealed class Opts(AlphaVantageOptions value) : IOptionsMonitor<AlphaVantageOptions>
    {
        public AlphaVantageOptions CurrentValue { get; } = value;
        public AlphaVantageOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AlphaVantageOptions, string?> listener) => null;
    }
}

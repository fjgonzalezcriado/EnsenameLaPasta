using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class TwelveDataHistoryProviderTests
{
    private static readonly Uri BaseUrl = new("https://api.twelvedata.com");

    private static TwelveDataHistoryProvider NewProvider(MockHttpMessageHandler handler, string apiKey = "demo")
        => new(new TestHttpClientFactory(handler, BaseUrl),
               new Opts(new TwelveDataOptions { ApiKey = apiKey }),
               TimeProvider.System,
               NullLogger<TwelveDataHistoryProvider>.Instance);

    // Twelve Data devuelve los valores del más reciente al más antiguo.
    private const string Json = """
        {"values":[
          {"datetime":"2026-07-06 15:30:00","close":"102.0","volume":"3000"},
          {"datetime":"2026-07-06 15:25:00","close":"101.0","volume":"2000"},
          {"datetime":"2026-07-06 15:20:00","close":"100.0","volume":"1000"}
        ],"status":"ok"}
        """;

    [Fact]
    public async Task GetHistoryAsync_OrdenaAscendente_YParseaPrecioYVolumen()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(Json));

        var points = await provider.GetHistoryAsync("AAPL", "1D");

        Assert.Equal(3, points.Count);
        Assert.True(points[0].Timestamp < points[2].Timestamp);   // ascendente tras reverse
        Assert.Equal(100m, points[0].Price);
        Assert.Equal(1000m, points[0].Volume);
        Assert.Equal(102m, points[2].Price);
        Assert.Equal(3000m, points[2].Volume);
    }

    [Fact]
    public async Task GetHistoryAsync_SinApiKey_Lanza()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("no debería llamar"));
        var provider = NewProvider(handler, apiKey: "");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetHistoryAsync("AAPL", "1D"));
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetHistoryAsync_RespuestaError_Lanza()
    {
        const string err = """{"code":404,"message":"symbol not found","status":"error"}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(err));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetHistoryAsync("XXXX", "1M"));
    }

    [Fact]
    public async Task GetHistoryAsync_RangoNoSoportado_Lanza()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(Json));
        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetHistoryAsync("AAPL", "10Y"));
    }

    private sealed class Opts : IOptionsMonitor<TwelveDataOptions>
    {
        public Opts(TwelveDataOptions value) => CurrentValue = value;
        public TwelveDataOptions CurrentValue { get; }
        public TwelveDataOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<TwelveDataOptions, string?> listener) => null;
    }
}

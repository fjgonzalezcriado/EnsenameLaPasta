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
          {"datetime":"2026-07-06 15:30:00","open":"101.5","high":"103.0","low":"101.0","close":"102.0","volume":"3000"},
          {"datetime":"2026-07-06 15:25:00","open":"100.5","high":"101.5","low":"100.0","close":"101.0","volume":"2000"},
          {"datetime":"2026-07-06 15:20:00","open":"99.0","high":"100.5","low":"98.5","close":"100.0","volume":"1000"}
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
    public async Task GetHistoryAsync_ParseaOHLC_ParaVelas()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(Json));

        var points = await provider.GetHistoryAsync("AAPL", "1D");

        // Tras ordenar ascendente, la 1ª barra es la más antigua (15:20).
        Assert.Equal(99.0m, points[0].Open);
        Assert.Equal(100.5m, points[0].High);
        Assert.Equal(98.5m, points[0].Low);
        Assert.Equal(100.0m, points[0].Price);
        Assert.Equal(101.5m, points[2].Open);
        Assert.Equal(103.0m, points[2].High);
        Assert.Equal(101.0m, points[2].Low);
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
    public async Task GetHistoryAsync_RespuestaError_DevuelveVacio()
    {
        const string err = """{"code":404,"message":"symbol not found","status":"error"}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(err));

        Assert.Empty(await provider.GetHistoryAsync("XXXX", "1M"));
    }

    [Fact]
    public async Task GetHistoryAsync_Http404_DevuelveVacio()
    {
        // Símbolo estilo Yahoo (p.ej. HY9H.F, ^GSPC) no válido en Twelve Data → 404.
        var provider = NewProvider(MockHttpMessageHandler.Status(System.Net.HttpStatusCode.NotFound));

        Assert.Empty(await provider.GetHistoryAsync("HY9H.F", "1D"));
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

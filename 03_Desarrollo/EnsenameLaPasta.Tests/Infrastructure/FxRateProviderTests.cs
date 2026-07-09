using System.Net;
using EnsenameLaPasta.Application.Common.Options;
using EnsenameLaPasta.Infrastructure.MarketData;
using EnsenameLaPasta.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Tests.Infrastructure;

public class FxRateProviderTests
{
    private static readonly Uri BaseUrl = new("https://query1.finance.yahoo.com");

    // Forma /v8/chart: el tipo de cambio vive en meta.regularMarketPrice.
    private const string UsdEurJson = """
        {"chart":{"result":[{"meta":{"symbol":"USDEUR=X","regularMarketPrice":0.92}}],"error":null}}
        """;

    private static FxRateProvider NewProvider(MockHttpMessageHandler handler, TimeProvider? time = null)
    {
        var factory = new TestHttpClientFactory(handler, BaseUrl);
        var opts = new StaticOptionsMonitor(new FxOptions { BaseCurrency = "EUR", CacheMinutes = 30 });
        return new FxRateProvider(factory, opts, time ?? TimeProvider.System, NullLogger<FxRateProvider>.Instance);
    }

    [Fact]
    public async Task GetRateAsync_MismaDivisa_Devuelve1SinLlamarHttp()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("no debería llamar"));
        var provider = NewProvider(handler);

        Assert.Equal(1m, await provider.GetRateAsync("EUR", "EUR"));
        Assert.Equal(1m, await provider.GetRateAsync("", "EUR"));
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetRateAsync_DevuelveTipoDeYahoo()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(UsdEurJson));

        var rate = await provider.GetRateAsync("usd", "eur");

        Assert.Equal(0.92m, rate);
    }

    [Fact]
    public async Task GetRateAsync_LlamaAlSimboloFxEsperado()
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(UsdEurJson, System.Text.Encoding.UTF8, "application/json")
        });
        var provider = NewProvider(handler);

        await provider.GetRateAsync("USD", "EUR");

        var request = Assert.Single(handler.ReceivedRequests);
        Assert.Contains("/v8/finance/chart/USDEUR=X", Uri.UnescapeDataString(request.RequestUri!.AbsolutePath));
    }

    [Fact]
    public async Task GetRateAsync_Cachea_NoRepiteLlamadaDentroDelTtl()
    {
        var calls = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(UsdEurJson, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var provider = NewProvider(handler);

        await provider.GetRateAsync("USD", "EUR");
        await provider.GetRateAsync("USD", "EUR");

        Assert.Equal(1, calls);   // segunda lectura desde caché
    }

    [Fact]
    public async Task RefreshAsync_FuerzaFetchAunqueEsteCacheado()
    {
        var calls = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(UsdEurJson, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var provider = NewProvider(handler);

        await provider.GetRateAsync("USD", "EUR");   // 1ª llamada, cachea
        await provider.RefreshAsync("USD", "EUR");   // fuerza fetch ignorando TTL
        await provider.GetRateAsync("USD", "EUR");   // lee de caché (no llama)

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task RefreshAsync_MismaDivisa_NoLlamaHttp()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("no debería llamar"));
        var provider = NewProvider(handler);

        await provider.RefreshAsync("EUR", "EUR");

        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetRateAsync_FallaHttp_DegradaA1()
    {
        var provider = NewProvider(MockHttpMessageHandler.Status(HttpStatusCode.InternalServerError));

        var rate = await provider.GetRateAsync("USD", "EUR");

        Assert.Equal(1m, rate);   // fallback (sin valor previo en caché)
    }

    private sealed class StaticOptionsMonitor(FxOptions value) : IOptionsMonitor<FxOptions>
    {
        public FxOptions CurrentValue { get; } = value;
        public FxOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<FxOptions, string?> listener) => null;
    }
}

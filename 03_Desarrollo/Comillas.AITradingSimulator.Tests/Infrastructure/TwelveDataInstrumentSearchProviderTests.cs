using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class TwelveDataInstrumentSearchProviderTests
{
    private static readonly Uri BaseUrl = new("https://api.twelvedata.com");

    private static TwelveDataInstrumentSearchProvider NewProvider(MockHttpMessageHandler handler)
        => new(new TestHttpClientFactory(handler, BaseUrl),
               new Opts(new TwelveDataOptions { ApiKey = "demo" }),
               NullLogger<TwelveDataInstrumentSearchProvider>.Instance);

    private const string Json = """
        {"data":[
          {"symbol":"AAPL","instrument_name":"Apple Inc","exchange":"NASDAQ","instrument_type":"Common Stock","currency":"USD"},
          {"symbol":"AAPL","instrument_name":"Apple Inc","exchange":"XETR","instrument_type":"Common Stock","currency":"EUR"}
        ],"status":"ok"}
        """;

    [Fact]
    public async Task SearchAsync_MapeaResultados()
    {
        var provider = NewProvider(MockHttpMessageHandler.Json(Json));

        var results = await provider.SearchAsync("apple");

        Assert.Equal(2, results.Count);
        Assert.Equal("AAPL", results[0].Symbol);
        Assert.Equal("Apple Inc", results[0].Name);
        Assert.Equal("NASDAQ", results[0].Exchange);
        Assert.Equal("Common Stock", results[0].Type);
        Assert.Equal("XETR", results[1].Exchange);
    }

    [Fact]
    public async Task SearchAsync_QueryVacia_DevuelveVacioSinLlamarHttp()
    {
        var handler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("no debería llamar"));
        var provider = NewProvider(handler);

        Assert.Empty(await provider.SearchAsync("  "));
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task SearchAsync_RespuestaError_DevuelveVacio()
    {
        const string err = """{"status":"error","message":"bad key"}""";
        var provider = NewProvider(MockHttpMessageHandler.Json(err));

        Assert.Empty(await provider.SearchAsync("aapl"));
    }

    private sealed class Opts(TwelveDataOptions value) : IOptionsMonitor<TwelveDataOptions>
    {
        public TwelveDataOptions CurrentValue { get; } = value;
        public TwelveDataOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<TwelveDataOptions, string?> listener) => null;
    }
}

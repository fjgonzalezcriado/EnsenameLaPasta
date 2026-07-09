using EnsenameLaPasta.Application.Common.Options;
using EnsenameLaPasta.Infrastructure.MarketData;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Tests.Infrastructure;

public sealed class MarketProviderStateTests : IDisposable
{
    private static readonly string FilePath = Path.Combine("App_Data", "active-provider.txt");

    public MarketProviderStateTests() => CleanFile();
    public void Dispose() => CleanFile();

    private static void CleanFile()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { /* best-effort */ }
    }

    private static MarketProviderState New(string defaultProvider)
        => new(Options.Create(new MarketDataOptions { ProviderType = defaultProvider }),
               NullLogger<MarketProviderState>.Instance);

    [Fact]
    public void Current_SinFichero_TomaElValorDeConfig()
    {
        Assert.Equal("YahooFinance", New("YahooFinance").Current);
        CleanFile();
        Assert.Equal("TwelveData", New("TwelveData").Current);
    }

    [Fact]
    public void Set_CambiaYNormaliza_YRechazaDesconocidos()
    {
        var state = New("YahooFinance");

        state.Set("twelvedata");                 // normaliza a TwelveData
        Assert.Equal("TwelveData", state.Current);

        state.Set("alphavantage");               // normaliza a AlphaVantage (3er proveedor)
        Assert.Equal("AlphaVantage", state.Current);

        state.Set("proveedor-inexistente");      // desconocido → fallback al primero
        Assert.Equal("YahooFinance", state.Current);

        Assert.Equal(3, state.Available.Count);
    }

    [Fact]
    public void Set_PersisteEntreInstancias()
    {
        New("YahooFinance").Set("TwelveData");

        // Nueva instancia con config por defecto Yahoo: debe leer el proveedor persistido.
        Assert.Equal("TwelveData", New("YahooFinance").Current);
    }
}

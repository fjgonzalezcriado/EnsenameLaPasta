using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;

namespace EnsenameLaPasta.Infrastructure.MarketData;

/// <summary>
/// Histórico que delega en el proveedor activo (resuelto en cada llamada), en coherencia
/// con el feed en vivo. Cambiable en runtime desde la UI.
/// </summary>
public sealed class SelectableMarketHistoryProvider(
    YahooHistoryProvider yahoo,
    TwelveDataHistoryProvider twelveData,
    AlphaVantageHistoryProvider alphaVantage,
    IMarketProviderState state) : IMarketHistoryProvider, IPreviousCloseProvider
{
    private readonly YahooHistoryProvider _yahoo = yahoo;
    private readonly TwelveDataHistoryProvider _twelveData = twelveData;
    private readonly AlphaVantageHistoryProvider _alphaVantage = alphaVantage;
    private readonly IMarketProviderState _state = state;

    public Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default)
    {
        var provider = _state.Current switch
        {
            var p when string.Equals(p, "TwelveData", StringComparison.OrdinalIgnoreCase) => (IMarketHistoryProvider)_twelveData,
            var p when string.Equals(p, "AlphaVantage", StringComparison.OrdinalIgnoreCase) => _alphaVantage,
            _ => _yahoo,
        };
        return provider.GetHistoryAsync(symbol, range, cancellationToken);
    }

    // Twelve Data / Alpha Vantage no exponen cierre anterior (HV-058): solo Yahoo lo soporta.
    // Con otro proveedor activo degrada a null. El consumidor cae a su comportamiento anterior.
    // Mismo criterio de "Yahoo por defecto" que GetHistoryAsync (el switch de arriba).
    public Task<decimal?> GetPreviousCloseAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var usesYahoo = _state.Current switch
        {
            var p when string.Equals(p, "TwelveData", StringComparison.OrdinalIgnoreCase) => false,
            var p when string.Equals(p, "AlphaVantage", StringComparison.OrdinalIgnoreCase) => false,
            _ => true,
        };
        return usesYahoo ? _yahoo.GetPreviousCloseAsync(symbol, cancellationToken) : Task.FromResult<decimal?>(null);
    }
}

using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;

namespace EnsenameLaPasta.Infrastructure.MarketData;

/// <summary>
/// Proveedor de datos en vivo que delega en el proveedor activo (resuelto en cada llamada
/// vía <see cref="IMarketProviderState"/>), permitiendo cambiarlo en runtime desde la UI.
/// </summary>
public sealed class SelectableMarketDataProvider(
    YahooFinanceProvider yahoo,
    TwelveDataProvider twelveData,
    AlphaVantageProvider alphaVantage,
    IMarketProviderState state) : IMarketDataProvider
{
    private readonly YahooFinanceProvider _yahoo = yahoo;
    private readonly TwelveDataProvider _twelveData = twelveData;
    private readonly AlphaVantageProvider _alphaVantage = alphaVantage;
    private readonly IMarketProviderState _state = state;

    public Task<MarketQuote> GetLatestAsync(string symbol, DateTime timestampUtc, CancellationToken cancellationToken = default)
    {
        var provider = _state.Current switch
        {
            var p when string.Equals(p, "TwelveData", StringComparison.OrdinalIgnoreCase) => (IMarketDataProvider)_twelveData,
            var p when string.Equals(p, "AlphaVantage", StringComparison.OrdinalIgnoreCase) => _alphaVantage,
            _ => _yahoo,
        };
        return provider.GetLatestAsync(symbol, timestampUtc, cancellationToken);
    }
}

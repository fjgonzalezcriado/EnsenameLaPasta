using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Buscador de instrumentos que delega en el del proveedor activo (Yahoo o Twelve Data),
/// para que los resultados usen la convención de símbolos del feed en uso.
/// </summary>
public sealed class SelectableInstrumentSearchProvider : IInstrumentSearchProvider
{
    private readonly YahooInstrumentSearchProvider _yahoo;
    private readonly TwelveDataInstrumentSearchProvider _twelveData;
    private readonly AlphaVantageInstrumentSearchProvider _alphaVantage;
    private readonly IMarketProviderState _state;

    public SelectableInstrumentSearchProvider(
        YahooInstrumentSearchProvider yahoo,
        TwelveDataInstrumentSearchProvider twelveData,
        AlphaVantageInstrumentSearchProvider alphaVantage,
        IMarketProviderState state)
    {
        _yahoo = yahoo;
        _twelveData = twelveData;
        _alphaVantage = alphaVantage;
        _state = state;
    }

    public Task<IReadOnlyList<InstrumentSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var provider = _state.Current switch
        {
            var p when string.Equals(p, "TwelveData", StringComparison.OrdinalIgnoreCase) => (IInstrumentSearchProvider)_twelveData,
            var p when string.Equals(p, "AlphaVantage", StringComparison.OrdinalIgnoreCase) => _alphaVantage,
            _ => _yahoo,
        };
        return provider.SearchAsync(query, cancellationToken);
    }
}

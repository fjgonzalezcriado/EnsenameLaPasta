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
    private readonly IMarketProviderState _state;

    public SelectableInstrumentSearchProvider(
        YahooInstrumentSearchProvider yahoo,
        TwelveDataInstrumentSearchProvider twelveData,
        IMarketProviderState state)
    {
        _yahoo = yahoo;
        _twelveData = twelveData;
        _state = state;
    }

    public Task<IReadOnlyList<InstrumentSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var provider = string.Equals(_state.Current, "TwelveData", StringComparison.OrdinalIgnoreCase)
            ? (IInstrumentSearchProvider)_twelveData
            : _yahoo;
        return provider.SearchAsync(query, cancellationToken);
    }
}

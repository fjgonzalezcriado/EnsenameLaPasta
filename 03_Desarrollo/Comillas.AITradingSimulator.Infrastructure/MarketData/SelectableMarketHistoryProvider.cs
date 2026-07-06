using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Histórico que delega en el proveedor activo (resuelto en cada llamada), en coherencia
/// con el feed en vivo. Cambiable en runtime desde la UI.
/// </summary>
public sealed class SelectableMarketHistoryProvider : IMarketHistoryProvider
{
    private readonly YahooHistoryProvider _yahoo;
    private readonly TwelveDataHistoryProvider _twelveData;
    private readonly IMarketProviderState _state;

    public SelectableMarketHistoryProvider(
        YahooHistoryProvider yahoo,
        TwelveDataHistoryProvider twelveData,
        IMarketProviderState state)
    {
        _yahoo = yahoo;
        _twelveData = twelveData;
        _state = state;
    }

    public Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default)
    {
        var provider = string.Equals(_state.Current, "TwelveData", StringComparison.OrdinalIgnoreCase)
            ? (IMarketHistoryProvider)_twelveData
            : _yahoo;
        return provider.GetHistoryAsync(symbol, range, cancellationToken);
    }
}

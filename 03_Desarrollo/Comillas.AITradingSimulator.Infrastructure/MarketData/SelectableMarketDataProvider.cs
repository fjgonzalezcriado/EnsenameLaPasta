using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Proveedor de datos en vivo que delega en el proveedor activo (resuelto en cada llamada
/// vía <see cref="IMarketProviderState"/>), permitiendo cambiarlo en runtime desde la UI.
/// </summary>
public sealed class SelectableMarketDataProvider : IMarketDataProvider
{
    private readonly YahooFinanceProvider _yahoo;
    private readonly TwelveDataProvider _twelveData;
    private readonly IMarketProviderState _state;

    public SelectableMarketDataProvider(
        YahooFinanceProvider yahoo,
        TwelveDataProvider twelveData,
        IMarketProviderState state)
    {
        _yahoo = yahoo;
        _twelveData = twelveData;
        _state = state;
    }

    public Task<MarketQuote> GetLatestAsync(string symbol, DateTime timestampUtc, CancellationToken cancellationToken = default)
    {
        var provider = string.Equals(_state.Current, "TwelveData", StringComparison.OrdinalIgnoreCase)
            ? (IMarketDataProvider)_twelveData
            : _yahoo;
        return provider.GetLatestAsync(symbol, timestampUtc, cancellationToken);
    }
}

using Comillas.AITradingSimulator.Application.Common.Dtos;

namespace Comillas.AITradingSimulator.Application.Common.Interfaces;

/// <summary>
/// Obtiene el histórico de precios (OHLC, cierre) de un símbolo para un rango
/// temporal dado (1D, 5D, 1M, 3M, 6M, YTD, 1A, 3A, 5A). Fuente: API externa real.
/// </summary>
public interface IMarketHistoryProvider
{
    Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default);
}

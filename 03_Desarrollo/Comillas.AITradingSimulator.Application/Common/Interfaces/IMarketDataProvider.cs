using Comillas.AITradingSimulator.Application.Common.Dtos;

namespace Comillas.AITradingSimulator.Application.Common.Interfaces;

public interface IMarketDataProvider
{
    /// <summary>
    /// Obtiene el último precio del símbolo como <see cref="MarketQuote"/> (tick + divisa).
    /// La implementación lo consulta a una fuente externa (Yahoo, Alpha, Binance…).
    /// </summary>
    /// <param name="symbol">Símbolo del activo (ej. AAPL, BTC-USD).</param>
    /// <param name="timestampUtc">Marca temporal UTC a usar en el tick.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<MarketQuote> GetLatestAsync(string symbol, DateTime timestampUtc, CancellationToken cancellationToken = default);
}

using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Domain.Enums;

namespace Comillas.AITradingSimulator.Application.Strategies;

public interface IStrategy
{
    /// <summary>
    /// Procesa un nuevo tick de mercado y opcionalmente emite una señal de trading.
    /// </summary>
    /// <returns>Señal a ejecutar o null si no hay decisión.</returns>
    TradeSignal? OnTick(MarketTick tick);
}

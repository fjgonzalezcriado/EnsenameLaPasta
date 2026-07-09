using EnsenameLaPasta.Domain.Entities;
using EnsenameLaPasta.Domain.Enums;

namespace EnsenameLaPasta.Application.Strategies;

public interface IStrategy
{
    /// <summary>
    /// Procesa un nuevo tick de mercado y opcionalmente emite una señal de trading.
    /// </summary>
    /// <returns>Señal a ejecutar o null si no hay decisión.</returns>
    TradeSignal? OnTick(MarketTick tick);
}

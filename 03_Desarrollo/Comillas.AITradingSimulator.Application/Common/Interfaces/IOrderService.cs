using Comillas.AITradingSimulator.Domain.Entities;

namespace Comillas.AITradingSimulator.Application.Common.Interfaces;

public interface IOrderService
{
    /// <summary>Abre un Trade si no hay posición abierta para el símbolo. Devuelve null si ya había una abierta.</summary>
    Task<Trade?> BuyAsync(string symbol, decimal entryPrice, decimal quantity, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Cierra el Trade abierto del símbolo. Devuelve null si no había ninguno.</summary>
    Task<Trade?> CloseAsync(string symbol, decimal exitPrice, DateTime nowUtc, CancellationToken cancellationToken = default);
}

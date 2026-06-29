using Comillas.AITradingSimulator.Domain.Entities;

namespace Comillas.AITradingSimulator.Application.Common.Dtos;

/// <summary>
/// Resultado de consultar el último precio de un símbolo: el tick a persistir
/// más la divisa de cotización del instrumento (cuando el proveedor la informa).
/// </summary>
/// <param name="Tick">Tick de mercado (símbolo, precio, volumen, timestamp).</param>
/// <param name="Currency">Divisa de cotización (ISO, p.ej. "EUR"); puede ser null si no se conoce.</param>
public sealed record MarketQuote(MarketTick Tick, string? Currency);

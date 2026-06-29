namespace Comillas.AITradingSimulator.Application.Common.Dtos;

/// <summary>
/// Resultado de búsqueda de un instrumento financiero (acción, ETF, índice…).
/// </summary>
/// <param name="Symbol">Símbolo de Yahoo Finance (p.ej. "HY9H.F"). Único para seguir el valor.</param>
/// <param name="Name">Nombre legible (longName, o shortName si no hay).</param>
/// <param name="Exchange">Bolsa donde cotiza (p.ej. "Frankfurt").</param>
/// <param name="Type">Tipo de instrumento (Equity, ETF, Index…).</param>
public sealed record InstrumentSearchResult(
    string Symbol,
    string Name,
    string Exchange,
    string Type);

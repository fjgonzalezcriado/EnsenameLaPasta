namespace Comillas.AITradingSimulator.Application.Common.Dtos;

/// <summary>Un punto de pronóstico: valor central y banda de confianza (HV-043).</summary>
public sealed record ForecastPoint(decimal Value, decimal LowerBound, decimal UpperBound);

/// <summary>
/// Resultado del pronóstico de precio (ML.NET SSA) para un símbolo y rango. La señal se deriva
/// del cambio esperado hasta el final del horizonte.
/// </summary>
/// <param name="Signal">"Alcista" | "Bajista" | "Neutral" | "Insuficiente".</param>
public sealed record PriceForecast(
    string Symbol,
    string Range,
    bool HasForecast,
    string Signal,
    decimal LastPrice,
    decimal ForecastEnd,
    decimal ExpectedChangePct,
    IReadOnlyList<ForecastPoint> Points,
    string? Message);

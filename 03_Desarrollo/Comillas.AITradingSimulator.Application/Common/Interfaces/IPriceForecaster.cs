using Comillas.AITradingSimulator.Application.Common.Dtos;

namespace Comillas.AITradingSimulator.Application.Common.Interfaces;

/// <summary>
/// Pronóstico de precio a corto plazo a partir del histórico del símbolo (HV-043).
/// La implementación usa ML.NET (SSA) y vive en Infrastructure.
/// </summary>
public interface IPriceForecaster
{
    /// <summary>
    /// Pronostica los próximos <paramref name="horizon"/> puntos del cierre de <paramref name="symbol"/>
    /// usando el histórico del rango indicado. No lanza: si no hay datos suficientes o el modelo
    /// falla, devuelve <see cref="PriceForecast.HasForecast"/> = false con un mensaje.
    /// </summary>
    Task<PriceForecast> ForecastAsync(string symbol, string range, int horizon, CancellationToken cancellationToken = default);
}

namespace Comillas.AITradingSimulator.Application.Common.Options;

/// <summary>
/// Configuración del proveedor de datos Alpha Vantage (https://www.alphavantage.co).
/// Se usa cuando <see cref="MarketDataOptions.ProviderType"/> == "AlphaVantage".
/// Requiere una API key. OJO: el plan gratuito es MUY limitado (25 peticiones/día),
/// insuficiente para el feed en vivo (sondea cada símbolo cada ~30 s); útil sobre todo
/// para búsquedas puntuales o histórico esporádico.
/// </summary>
public sealed class AlphaVantageOptions
{
    public const string SectionName = "MarketData:AlphaVantage";

    /// <summary>URL base del API de Alpha Vantage.</summary>
    public string BaseUrl { get; set; } = "https://www.alphavantage.co";

    /// <summary>API key personal (obligatoria si el proveedor activo es AlphaVantage).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Timeout HTTP en segundos.</summary>
    public int TimeoutSeconds { get; set; } = 10;
}

namespace EnsenameLaPasta.Application.Common.Options;

/// <summary>
/// Configuración del proveedor de datos Twelve Data (https://twelvedata.com).
/// Se usa cuando <see cref="MarketDataOptions.ProviderType"/> == "TwelveData".
/// Requiere una API key (plan gratuito: 8 req/min, 800/día).
/// </summary>
public sealed class TwelveDataOptions
{
    public const string SectionName = "MarketData:TwelveData";

    /// <summary>URL base del API de Twelve Data.</summary>
    public string BaseUrl { get; set; } = "https://api.twelvedata.com";

    /// <summary>API key personal (obligatoria si el proveedor activo es TwelveData).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Timeout HTTP en segundos.</summary>
    public int TimeoutSeconds { get; set; } = 10;
}

namespace Comillas.AITradingSimulator.Application.Common.Options;

/// <summary>
/// Configuración de conversión de divisas. Los totales de cuenta se expresan en
/// <see cref="BaseCurrency"/>; los tipos de cambio se cachean <see cref="CacheMinutes"/>.
/// </summary>
public sealed class FxOptions
{
    public const string SectionName = "Fx";

    /// <summary>Divisa base de los totales (ISO, p.ej. "EUR").</summary>
    public string BaseCurrency { get; set; } = "EUR";

    /// <summary>Minutos de validez de un tipo de cambio en caché.</summary>
    public int CacheMinutes { get; set; } = 30;

    /// <summary>Cada cuántos segundos el servicio en background refresca los tipos en uso.</summary>
    public int RefreshSeconds { get; set; } = 300;
}

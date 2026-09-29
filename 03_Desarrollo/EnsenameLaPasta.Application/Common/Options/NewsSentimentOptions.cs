namespace EnsenameLaPasta.Application.Common.Options;

/// <summary>
/// Configuración de la captura periódica de sentimiento de noticias (HV-056): cada
/// <see cref="IntervalSeconds"/> se recorre la watchlist y se persiste el agregado del día
/// para cada símbolo, sentando la base de histórico para un futuro uso como feature ML.
/// </summary>
public sealed class NewsSentimentOptions
{
    public const string SectionName = "NewsSentiment";

    /// <summary>Activa/desactiva la captura periódica.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Cada cuántos segundos recorrer la watchlist y capturar. Por defecto 6 horas.</summary>
    public int IntervalSeconds { get; set; } = 21600;

    /// <summary>Si se captura poco después de arrancar (para no esperar 6h al primer dato).</summary>
    public bool CaptureOnStartup { get; set; } = true;

    /// <summary>Pausa entre símbolos dentro de una pasada, para no encadenar peticiones a Yahoo.</summary>
    public int DelayBetweenSymbolsMs { get; set; } = 500;
}

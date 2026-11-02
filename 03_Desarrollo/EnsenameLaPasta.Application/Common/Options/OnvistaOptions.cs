namespace EnsenameLaPasta.Application.Common.Options;

/// <summary>
/// Configuración del proveedor de noticias de onvista.de (HV-057). onvista no tiene API
/// pública, pero expone un feed RSS filtrable por instrumento
/// (<c>/news/feed/rss.xml?entityType=..&amp;entityValue=..</c>) donde <c>entityValue</c> es un
/// ID interno propietario que no se puede resolver desde el ticker: hay que localizarlo
/// manualmente en la página del instrumento en onvista.de y añadirlo a <see cref="SymbolMap"/>.
/// </summary>
public sealed class OnvistaOptions
{
    public const string SectionName = "Onvista";

    /// <summary>URL base de onvista.de.</summary>
    public string BaseUrl { get; set; } = "https://www.onvista.de";

    /// <summary>Timeout HTTP en segundos.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Mapeo símbolo de la watchlist → identificador interno de onvista. Un símbolo sin entrada
    /// aquí simplemente no aporta noticias de onvista al combinado (degradación silenciosa).
    /// </summary>
    public Dictionary<string, OnvistaSymbolMapping> SymbolMap { get; set; } = [];
}

/// <summary>Identificador de instrumento en onvista.de para el feed RSS filtrado.</summary>
public sealed class OnvistaSymbolMapping
{
    /// <summary>"STOCK" o "INDEX", tal como aparece en la URL del feed de onvista.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>ID numérico interno del instrumento en onvista.de.</summary>
    public long EntityValue { get; set; }
}

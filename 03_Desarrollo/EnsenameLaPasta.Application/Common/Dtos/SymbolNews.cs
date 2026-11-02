namespace EnsenameLaPasta.Application.Common.Dtos;

/// <summary>Un titular de noticia asociado a uno o más símbolos (HV-055).</summary>
/// <param name="Source">Fuente que aportó el titular ("Yahoo" | "onvista", HV-057), para
/// distinguir el origen cuando se combinan varias fuentes en una lista mezclada.</param>
public sealed record NewsItem(
    string Title,
    string? Publisher,
    string Link,
    DateTime? PublishedAtUtc,
    IReadOnlyList<string> RelatedTickers,
    string Source = "Yahoo");

/// <summary>
/// Resultado de pedir noticias para un símbolo. No lanza: si no hay cobertura (símbolo poco
/// seguido, fuente caída) devuelve <see cref="HasNews"/> = false con un mensaje, igual que
/// <c>PriceForecast</c>/<c>DirectionSignal</c> (HV-043/044).
/// </summary>
public sealed record SymbolNews(
    string Symbol,
    bool HasNews,
    IReadOnlyList<NewsItem> Items,
    string? Message);

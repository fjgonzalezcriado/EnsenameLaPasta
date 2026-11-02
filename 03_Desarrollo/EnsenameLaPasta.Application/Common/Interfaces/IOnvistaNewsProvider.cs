using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Noticias de onvista.de por símbolo (HV-057), vía su feed RSS filtrado por instrumento.
/// La implementación vive en Infrastructure. A diferencia de <see cref="INewsProvider"/>, no
/// hay búsqueda por símbolo: onvista identifica cada instrumento con un ID interno propio que
/// no se puede resolver desde el ticker, así que solo cubre los símbolos mapeados en
/// configuración (<c>Onvista:SymbolMap</c>).
/// </summary>
public interface IOnvistaNewsProvider
{
    /// <summary>
    /// Devuelve hasta <paramref name="max"/> titulares recientes para <paramref name="symbol"/>.
    /// No lanza: sin mapeo configurado, sin cobertura o si la fuente falla, devuelve una lista
    /// vacía (el agregador decide el mensaje combinado).
    /// </summary>
    Task<IReadOnlyList<NewsItem>> GetNewsAsync(string symbol, int max = 8, CancellationToken cancellationToken = default);
}

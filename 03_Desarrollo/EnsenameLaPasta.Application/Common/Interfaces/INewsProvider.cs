using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Noticias recientes asociadas a un símbolo (HV-055). La implementación vive en Infrastructure.
/// </summary>
public interface INewsProvider
{
    /// <summary>
    /// Devuelve hasta <paramref name="max"/> titulares recientes para <paramref name="symbol"/>.
    /// No lanza: si no hay cobertura o la fuente falla, devuelve <see cref="SymbolNews.HasNews"/> = false
    /// con un mensaje.
    /// </summary>
    Task<SymbolNews> GetNewsAsync(string symbol, int max = 8, CancellationToken cancellationToken = default);
}

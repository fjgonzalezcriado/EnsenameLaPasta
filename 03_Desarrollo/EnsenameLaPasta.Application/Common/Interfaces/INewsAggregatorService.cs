using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Combina las noticias de todas las fuentes disponibles para un símbolo (HV-057): Yahoo
/// (<see cref="INewsProvider"/>, HV-055) + onvista (<see cref="IOnvistaNewsProvider"/>) en una
/// sola lista ordenada por fecha. No lanza: si ninguna fuente aporta nada, degrada igual que
/// antes (<see cref="SymbolNews.HasNews"/> = false).
/// </summary>
public interface INewsAggregatorService
{
    Task<SymbolNews> GetNewsAsync(string symbol, int max = 8, CancellationToken cancellationToken = default);
}

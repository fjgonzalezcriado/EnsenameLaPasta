using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Gestiona la lista de instrumentos seguidos (watchlist) que el panel muestra
/// y que el generador de ticks consulta para pedir datos reales.
/// </summary>
public interface IWatchlistService
{
    Task<IReadOnlyList<TrackedSymbolDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Añade un símbolo. Devuelve false si ya estaba en la watchlist.</summary>
    Task<bool> AddAsync(string symbol, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deja de seguir un símbolo y borra su histórico de ticks (para que
    /// desaparezca del panel). Devuelve false si no estaba.
    /// </summary>
    Task<bool> RemoveAsync(string symbol, CancellationToken cancellationToken = default);
}

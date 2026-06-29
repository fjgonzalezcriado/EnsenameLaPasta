using Comillas.AITradingSimulator.Application.Common.Dtos;

namespace Comillas.AITradingSimulator.Application.Common.Interfaces;

/// <summary>
/// Gestión manual de posiciones reales del usuario (alta/cierre/borrado por Id).
/// A diferencia de <see cref="IOrderService"/> (estrategia automática), permite
/// varias posiciones abiertas sobre el mismo símbolo.
/// </summary>
public interface IPositionService
{
    /// <summary>
    /// Abre una posición manual. Asegura que el símbolo esté en la watchlist
    /// (para obtener precio en vivo). Devuelve el Id de la posición creada.
    /// </summary>
    Task<Guid> OpenAsync(string symbol, decimal entryPrice, decimal quantity, DateTime? openedAtUtc = null, CancellationToken cancellationToken = default);

    /// <summary>Cierra una posición abierta por Id. Devuelve false si no existe o ya está cerrada.</summary>
    Task<bool> CloseAsync(Guid id, decimal exitPrice, DateTime? closedAtUtc = null, CancellationToken cancellationToken = default);

    /// <summary>Elimina una posición por Id (corrección de errores). Devuelve false si no existe.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Importa posiciones desde un CSV (symbol, entry, qty[, date]). Las filas con
    /// error se reportan y no abortan el resto. Devuelve el recuento y los errores.
    /// </summary>
    Task<ImportResultDto> ImportCsvAsync(string csv, CancellationToken cancellationToken = default);
}

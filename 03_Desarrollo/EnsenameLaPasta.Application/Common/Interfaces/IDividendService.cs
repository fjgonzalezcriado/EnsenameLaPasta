using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Gestiona los dividendos cobrados (HV-050). Suman al PnL realizado y al efectivo; la conversión
/// a divisa base y su integración en los totales vive en el dashboard.
/// </summary>
public interface IDividendService
{
    Task<IReadOnlyList<DividendDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Registra un dividendo (importe &gt; 0) para un símbolo. Devuelve su Id.</summary>
    Task<Guid> AddAsync(string symbol, decimal amount, DateTime? receivedAtUtc = null, string? currency = "EUR", string? note = null, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

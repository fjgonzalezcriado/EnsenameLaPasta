using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Gestiona los movimientos de caja (ingresos/retiradas) del usuario.
/// El efectivo disponible se deriva de estos movimientos más el efecto de las
/// operaciones (compras restan, ventas suman); ese cálculo vive en el dashboard.
/// </summary>
public interface ICashService
{
    Task<IReadOnlyList<CashMovementDto>> GetMovementsAsync(CancellationToken cancellationToken = default);

    /// <summary>Suma neta de movimientos en importe bruto (sin convertir divisas).</summary>
    Task<decimal> GetNetDepositsAsync(CancellationToken cancellationToken = default);

    /// <summary>Registra un movimiento (importe &gt; 0 ingreso, &lt; 0 retirada) en la divisa indicada. Devuelve su Id.</summary>
    Task<Guid> AddAsync(decimal amount, string? note, DateTime? createdAtUtc = null, string? currency = "EUR", CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

namespace EnsenameLaPasta.Application.Common.Dtos;

/// <summary>Movimiento de caja (ingreso si Amount &gt; 0, retirada si &lt; 0).</summary>
public sealed record CashMovementDto(Guid Id, decimal Amount, string Note, string Currency, DateTime CreatedAt);

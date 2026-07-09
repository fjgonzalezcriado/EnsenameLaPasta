namespace EnsenameLaPasta.Domain.Entities;

/// <summary>
/// Movimiento de caja del usuario: ingreso (importe &gt; 0) o retirada (importe &lt; 0).
/// El efectivo disponible se deriva de estos movimientos y del efecto de las operaciones.
/// </summary>
public sealed class CashMovement
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public string Note { get; private set; } = string.Empty;

    /// <summary>Divisa del movimiento (ISO, p.ej. "EUR", "USD"). Por defecto EUR.</summary>
    public string Currency { get; private set; } = "EUR";

    public DateTime CreatedAt { get; private set; }

    private CashMovement() { }

    public static CashMovement Create(decimal amount, string? note, DateTime createdAtUtc, string? currency = "EUR")
    {
        if (amount == 0)
            throw new ArgumentException("El importe del movimiento no puede ser 0.", nameof(amount));

        var ccy = (currency ?? string.Empty).Trim().ToUpperInvariant();

        return new CashMovement
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Note = (note ?? string.Empty).Trim(),
            Currency = ccy.Length == 0 ? "EUR" : ccy,
            CreatedAt = createdAtUtc
        };
    }
}

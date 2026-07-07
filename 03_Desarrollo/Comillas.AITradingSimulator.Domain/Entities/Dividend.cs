namespace Comillas.AITradingSimulator.Domain.Entities;

/// <summary>
/// Dividendo cobrado por un instrumento (HV-050). Es ingreso de rentabilidad (suma al PnL y al
/// efectivo), no una aportación de capital. Lleva su divisa (se convierte a base al agregar).
/// </summary>
public sealed class Dividend
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public DateTime ReceivedAt { get; private set; }
    public string Note { get; private set; } = string.Empty;

    private Dividend() { }

    public static Dividend Create(string symbol, decimal amount, DateTime receivedAtUtc, string? currency = "EUR", string? note = null)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "El importe del dividendo debe ser > 0.");

        var ccy = (currency ?? string.Empty).Trim().ToUpperInvariant();

        return new Dividend
        {
            Id = Guid.NewGuid(),
            Symbol = symbol.Trim().ToUpperInvariant(),
            Amount = amount,
            Currency = ccy.Length == 0 ? "EUR" : ccy,
            ReceivedAt = receivedAtUtc,
            Note = (note ?? string.Empty).Trim()
        };
    }
}

namespace EnsenameLaPasta.Domain.Entities;

/// <summary>
/// Instrumento seguido en el panel (watchlist). El generador de ticks consulta
/// esta tabla en cada ciclo para saber qué símbolos pedir al proveedor real.
/// </summary>
public sealed class TrackedSymbol
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime AddedAt { get; private set; }

    /// <summary>Divisa de cotización del instrumento (ISO, p.ej. "EUR", "USD"). Vacía hasta conocerse.</summary>
    public string Currency { get; private set; } = string.Empty;

    private TrackedSymbol() { }

    public static TrackedSymbol Create(string symbol, string name, DateTime addedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));

        return new TrackedSymbol
        {
            Id = Guid.NewGuid(),
            Symbol = symbol.Trim().ToUpperInvariant(),
            Name = (name ?? string.Empty).Trim(),
            AddedAt = addedAtUtc,
            Currency = string.Empty
        };
    }

    /// <summary>Fija la divisa de cotización (normalizada a mayúsculas). Ignora valores vacíos.</summary>
    public void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return;
        Currency = currency.Trim().ToUpperInvariant();
    }
}

namespace Comillas.AITradingSimulator.Domain.Entities;

public sealed class MarketTick
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public decimal Volume { get; private set; }
    public DateTime Timestamp { get; private set; }

    private MarketTick() { }

    public static MarketTick Create(string symbol, decimal price, decimal volume, DateTime timestampUtc)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price debe ser > 0.");
        if (volume < 0)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume no puede ser negativo.");

        return new MarketTick
        {
            Id = Guid.NewGuid(),
            Symbol = symbol.ToUpperInvariant(),
            Price = price,
            Volume = volume,
            Timestamp = timestampUtc
        };
    }
}

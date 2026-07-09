using EnsenameLaPasta.Domain.Enums;

namespace EnsenameLaPasta.Domain.Entities;

public sealed class Trade
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public decimal EntryPrice { get; private set; }
    public decimal? ExitPrice { get; private set; }
    public decimal Quantity { get; private set; }
    public TradeStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    /// <summary>Comisiones acumuladas del trade (apertura + cierre) en divisa base (HV-050).</summary>
    public decimal Commission { get; private set; }

    private Trade() { }

    /// <summary>Suma una comisión (p.ej. la tarifa por orden del bróker). No negativa.</summary>
    public void AddCommission(decimal fee)
    {
        if (fee < 0)
            throw new ArgumentOutOfRangeException(nameof(fee), "La comisión no puede ser negativa.");
        Commission += fee;
    }

    public static Trade Open(string symbol, decimal entryPrice, decimal quantity, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));
        if (entryPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(entryPrice), "EntryPrice debe ser > 0.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity debe ser > 0.");

        return new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = symbol.ToUpperInvariant(),
            EntryPrice = entryPrice,
            Quantity = quantity,
            Status = TradeStatus.Open,
            CreatedAt = createdAtUtc
        };
    }

    public void Close(decimal exitPrice, DateTime closedAtUtc)
    {
        if (Status == TradeStatus.Closed)
            throw new InvalidOperationException("Trade ya está cerrado.");
        if (exitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(exitPrice), "ExitPrice debe ser > 0.");
        if (closedAtUtc < CreatedAt)
            throw new ArgumentException("ClosedAt no puede ser anterior a CreatedAt.", nameof(closedAtUtc));

        ExitPrice = exitPrice;
        ClosedAt = closedAtUtc;
        Status = TradeStatus.Closed;
    }

    public decimal? RealizedPnL => Status == TradeStatus.Closed
        ? (ExitPrice!.Value - EntryPrice) * Quantity
        : null;
}

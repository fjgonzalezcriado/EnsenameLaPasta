namespace EnsenameLaPasta.Domain.Entities;

public sealed class PortfolioSnapshot
{
    public Guid Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public decimal Capital { get; private set; }
    public decimal RealizedPnL { get; private set; }
    public decimal UnrealizedPnL { get; private set; }
    public int OpenPositions { get; private set; }
    public int ClosedTrades { get; private set; }

    private PortfolioSnapshot() { }

    public static PortfolioSnapshot Create(
        DateTime timestampUtc,
        decimal capital,
        decimal realizedPnL,
        decimal unrealizedPnL,
        int openPositions,
        int closedTrades)
    {
        if (capital < 0)
            throw new ArgumentOutOfRangeException(nameof(capital), "Capital no puede ser negativo.");
        if (openPositions < 0)
            throw new ArgumentOutOfRangeException(nameof(openPositions), "OpenPositions no puede ser negativo.");
        if (closedTrades < 0)
            throw new ArgumentOutOfRangeException(nameof(closedTrades), "ClosedTrades no puede ser negativo.");

        return new PortfolioSnapshot
        {
            Id = Guid.NewGuid(),
            Timestamp = timestampUtc,
            Capital = capital,
            RealizedPnL = realizedPnL,
            UnrealizedPnL = unrealizedPnL,
            OpenPositions = openPositions,
            ClosedTrades = closedTrades
        };
    }

    public decimal TotalPnL => RealizedPnL + UnrealizedPnL;
}

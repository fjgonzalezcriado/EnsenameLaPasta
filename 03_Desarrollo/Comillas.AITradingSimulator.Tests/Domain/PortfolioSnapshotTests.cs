using Comillas.AITradingSimulator.Domain.Entities;

namespace Comillas.AITradingSimulator.Tests.Domain;

public class PortfolioSnapshotTests
{
    private static readonly DateTime BaseTime = new(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_DatosValidos_DevuelveSnapshot()
    {
        var snapshot = PortfolioSnapshot.Create(BaseTime, 10_000m, 500m, 200m, 3, 12);

        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(BaseTime, snapshot.Timestamp);
        Assert.Equal(10_000m, snapshot.Capital);
        Assert.Equal(500m, snapshot.RealizedPnL);
        Assert.Equal(200m, snapshot.UnrealizedPnL);
        Assert.Equal(3, snapshot.OpenPositions);
        Assert.Equal(12, snapshot.ClosedTrades);
    }

    [Fact]
    public void Create_CapitalNegativo_Lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PortfolioSnapshot.Create(BaseTime, -1m, 0m, 0m, 0, 0));
    }

    [Fact]
    public void Create_OpenPositionsNegativo_Lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PortfolioSnapshot.Create(BaseTime, 1000m, 0m, 0m, -1, 0));
    }

    [Fact]
    public void Create_ClosedTradesNegativo_Lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PortfolioSnapshot.Create(BaseTime, 1000m, 0m, 0m, 0, -5));
    }

    [Fact]
    public void Create_PnLNegativoPermitido()
    {
        // RealizedPnL y UnrealizedPnL pueden ser negativos (pérdidas)
        var snapshot = PortfolioSnapshot.Create(BaseTime, 9000m, -300m, -150m, 1, 5);

        Assert.Equal(-300m, snapshot.RealizedPnL);
        Assert.Equal(-150m, snapshot.UnrealizedPnL);
    }

    [Fact]
    public void TotalPnL_SumaRealizedYUnrealized()
    {
        var snapshot = PortfolioSnapshot.Create(BaseTime, 10_000m, 500m, 200m, 3, 12);
        Assert.Equal(700m, snapshot.TotalPnL);
    }

    [Fact]
    public void TotalPnL_ConPerdidas_ResultaNegativo()
    {
        var snapshot = PortfolioSnapshot.Create(BaseTime, 9000m, -300m, -150m, 1, 5);
        Assert.Equal(-450m, snapshot.TotalPnL);
    }
}

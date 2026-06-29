using Comillas.AITradingSimulator.Domain.Entities;

namespace Comillas.AITradingSimulator.Tests.Domain;

public class MarketTickTests
{
    private static readonly DateTime BaseTime = new(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_DatosValidos_DevuelveTick()
    {
        var tick = MarketTick.Create("AAPL", 150.50m, 1000m, BaseTime);

        Assert.NotEqual(Guid.Empty, tick.Id);
        Assert.Equal("AAPL", tick.Symbol);
        Assert.Equal(150.50m, tick.Price);
        Assert.Equal(1000m, tick.Volume);
        Assert.Equal(BaseTime, tick.Timestamp);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_SymbolVacio_LanzaArgumentException(string? symbol)
    {
        Assert.Throws<ArgumentException>(() => MarketTick.Create(symbol!, 100m, 1m, BaseTime));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_PriceMenorOIgualACero_Lanza(decimal price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarketTick.Create("AAPL", price, 1m, BaseTime));
    }

    [Fact]
    public void Create_VolumeNegativo_Lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarketTick.Create("AAPL", 100m, -1m, BaseTime));
    }

    [Fact]
    public void Create_VolumeCero_EsValido()
    {
        // Boundary: volume = 0 está permitido (>= 0)
        var tick = MarketTick.Create("AAPL", 100m, 0m, BaseTime);
        Assert.Equal(0m, tick.Volume);
    }

    [Fact]
    public void Create_SymbolEnMinusculas_GuardadoEnMayusculas()
    {
        var tick = MarketTick.Create("aapl", 100m, 1m, BaseTime);
        Assert.Equal("AAPL", tick.Symbol);
    }
}

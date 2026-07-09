using EnsenameLaPasta.Domain.Entities;
using EnsenameLaPasta.Domain.Enums;

namespace EnsenameLaPasta.Tests.Domain;

public class TradeTests
{
    private static readonly DateTime BaseTime = new(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Open_SymbolVacio_LanzaArgumentException(string? symbol)
    {
        Assert.Throws<ArgumentException>(() => Trade.Open(symbol!, 100m, 1m, BaseTime));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.0001)]
    public void Open_EntryPriceMenorOIgualACero_LanzaArgumentOutOfRangeException(decimal entryPrice)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Trade.Open("AAPL", entryPrice, 1m, BaseTime));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void Open_QuantityMenorOIgualACero_LanzaArgumentOutOfRangeException(decimal quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Trade.Open("AAPL", 100m, quantity, BaseTime));
    }

    [Fact]
    public void Open_DatosValidos_TradeEnEstadoOpen()
    {
        var trade = Trade.Open("AAPL", 150.50m, 10m, BaseTime);

        Assert.NotEqual(Guid.Empty, trade.Id);
        Assert.Equal("AAPL", trade.Symbol);
        Assert.Equal(150.50m, trade.EntryPrice);
        Assert.Equal(10m, trade.Quantity);
        Assert.Equal(TradeStatus.Open, trade.Status);
        Assert.Equal(BaseTime, trade.CreatedAt);
        Assert.Null(trade.ExitPrice);
        Assert.Null(trade.ClosedAt);
    }

    [Fact]
    public void Open_SymbolEnMinusculas_GuardadoEnMayusculas()
    {
        var trade = Trade.Open("aapl", 100m, 1m, BaseTime);
        Assert.Equal("AAPL", trade.Symbol);
    }

    [Fact]
    public void Close_TradeYaCerrado_LanzaInvalidOperationException()
    {
        var trade = Trade.Open("AAPL", 100m, 1m, BaseTime);
        trade.Close(110m, BaseTime.AddHours(1));

        Assert.Throws<InvalidOperationException>(() => trade.Close(120m, BaseTime.AddHours(2)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Close_ExitPriceMenorOIgualACero_Lanza(decimal exitPrice)
    {
        var trade = Trade.Open("AAPL", 100m, 1m, BaseTime);
        Assert.Throws<ArgumentOutOfRangeException>(() => trade.Close(exitPrice, BaseTime.AddHours(1)));
    }

    [Fact]
    public void Close_ClosedAntesQueCreated_LanzaArgumentException()
    {
        var trade = Trade.Open("AAPL", 100m, 1m, BaseTime);
        Assert.Throws<ArgumentException>(() => trade.Close(110m, BaseTime.AddHours(-1)));
    }

    [Fact]
    public void Close_DatosValidos_TradeEnEstadoClosed()
    {
        var trade = Trade.Open("AAPL", 100m, 10m, BaseTime);
        var closedAt = BaseTime.AddHours(2);

        trade.Close(110m, closedAt);

        Assert.Equal(TradeStatus.Closed, trade.Status);
        Assert.Equal(110m, trade.ExitPrice);
        Assert.Equal(closedAt, trade.ClosedAt);
    }

    [Fact]
    public void RealizedPnL_TradeAbierto_RetornaNull()
    {
        var trade = Trade.Open("AAPL", 100m, 10m, BaseTime);
        Assert.Null(trade.RealizedPnL);
    }

    [Fact]
    public void RealizedPnL_TradeCerradoConGanancia_RetornaPositivo()
    {
        var trade = Trade.Open("AAPL", 100m, 10m, BaseTime);
        trade.Close(115m, BaseTime.AddHours(1));

        // (115 - 100) * 10 = 150
        Assert.Equal(150m, trade.RealizedPnL);
    }

    [Fact]
    public void RealizedPnL_TradeCerradoConPerdida_RetornaNegativo()
    {
        var trade = Trade.Open("AAPL", 100m, 10m, BaseTime);
        trade.Close(90m, BaseTime.AddHours(1));

        // (90 - 100) * 10 = -100
        Assert.Equal(-100m, trade.RealizedPnL);
    }

    [Fact]
    public void Close_ClosedAtIgualACreatedAt_PermiteCerrar()
    {
        // Boundary: el spec dice "no anterior", igual sí se permite
        var trade = Trade.Open("AAPL", 100m, 1m, BaseTime);
        trade.Close(110m, BaseTime);

        Assert.Equal(TradeStatus.Closed, trade.Status);
    }
}

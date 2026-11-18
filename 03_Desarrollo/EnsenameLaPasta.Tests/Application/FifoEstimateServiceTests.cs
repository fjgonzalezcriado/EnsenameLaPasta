using EnsenameLaPasta.Application.Common.Options;
using EnsenameLaPasta.Application.Services;
using EnsenameLaPasta.Domain.Entities;
using EnsenameLaPasta.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Tests.Application;

public sealed class FifoEstimateServiceTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2026, 6, 16, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public FifoEstimateServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TradingDbContext>().UseSqlite(_connection).Options;
        using var ctx = new TradingDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private TradingDbContext NewContext() => new(_options);

    private static FifoEstimateService NewService(TradingDbContext ctx, decimal sellFee = 1m)
        => new(ctx, Options.Create(new BrokerOptions { CommissionPerOrder = sellFee }));

    private static Trade OpenLot(string symbol, decimal entry, decimal qty, DateTime createdAt, decimal buyCommission = 0m)
    {
        var trade = Trade.Open(symbol, entry, qty, createdAt);
        if (buyCommission > 0m) trade.AddCommission(buyCommission);
        return trade;
    }

    [Fact]
    public async Task EstimateAsync_SinPosicionesAbiertas_DevuelveMensajeYCeros()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        var result = await sut.EstimateAsync("AAA", 10m);

        Assert.Equal(0m, result.QuantityMatched);
        Assert.Empty(result.Lots);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task EstimateAsync_UnSoloLote_ConsumeToDoYAplicaComisionDeVenta()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(OpenLot("AAA", 100m, 10m, BaseTime));
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var result = await NewService(verify, sellFee: 1m).EstimateAsync("AAA", 10m, exitPrice: 120m);

        Assert.Equal(10m, result.QuantityMatched);
        Assert.Single(result.Lots);
        Assert.Equal(1000m, result.TotalCostBasis);   // 100 * 10
        Assert.Equal(1200m, result.TotalProceeds);    // 120 * 10
        Assert.Equal(1m, result.SellCommission);
        Assert.Equal(199m, result.EstimatedGain);     // 1200 - 1000 - 1
    }

    [Fact]
    public async Task EstimateAsync_VariosLotes_ConsumeElMasAntiguoPrimero()
    {
        // Lote antiguo: 5 @ 100 (16/06). Lote reciente: 5 @ 200 (17/06).
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(OpenLot("AAA", 100m, 5m, BaseTime));
            ctx.Trades.Add(OpenLot("AAA", 200m, 5m, BaseTime.AddDays(1)));
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        // Vender 5 unidades: FIFO debe tomarlas del lote antiguo (100), no del reciente (200).
        var result = await NewService(verify, sellFee: 0m).EstimateAsync("AAA", 5m, exitPrice: 150m);

        Assert.Equal(5m, result.QuantityMatched);
        Assert.Single(result.Lots);
        Assert.Equal(100m, result.Lots[0].EntryPrice);
        Assert.Equal(500m, result.TotalCostBasis);    // 100 * 5
        Assert.Equal(750m, result.TotalProceeds);     // 150 * 5
        Assert.Equal(250m, result.EstimatedGain);
    }

    [Fact]
    public async Task EstimateAsync_CantidadQueCruzaDosLotes_PartePropociaonalmenteElSegundoLote()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(OpenLot("AAA", 100m, 5m, BaseTime));           // lote 1: 5 @ 100
            ctx.Trades.Add(OpenLot("AAA", 200m, 5m, BaseTime.AddDays(1))); // lote 2: 5 @ 200
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        // Vender 7: 5 del lote 1 (antiguo) + 2 del lote 2 (reciente).
        var result = await NewService(verify, sellFee: 0m).EstimateAsync("AAA", 7m, exitPrice: 150m);

        Assert.Equal(7m, result.QuantityMatched);
        Assert.Equal(2, result.Lots.Count);
        Assert.Equal(5m, result.Lots[0].QuantityUsed);
        Assert.Equal(100m, result.Lots[0].EntryPrice);
        Assert.Equal(2m, result.Lots[1].QuantityUsed);
        Assert.Equal(200m, result.Lots[1].EntryPrice);

        // Coste: 5*100 + 2*200 = 900. Producto: 7*150 = 1050. Ganancia: 150.
        Assert.Equal(900m, result.TotalCostBasis);
        Assert.Equal(1050m, result.TotalProceeds);
        Assert.Equal(150m, result.EstimatedGain);
    }

    [Fact]
    public async Task EstimateAsync_CantidadSuperiorALaAbierta_MarcaSoloLaDisponibleYAvisa()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(OpenLot("AAA", 100m, 5m, BaseTime));
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var result = await NewService(verify).EstimateAsync("AAA", 10m, exitPrice: 120m);

        Assert.Equal(5m, result.QuantityMatched);
        Assert.Equal(10m, result.QuantityRequested);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task EstimateAsync_ComisionDeCompraSeProrrateaConLaCantidadConsumida()
    {
        // Lote de 10 unidades con 1€ de comisión de compra; se vende solo la mitad (5).
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(OpenLot("AAA", 100m, 10m, BaseTime, buyCommission: 1m));
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var result = await NewService(verify, sellFee: 0m).EstimateAsync("AAA", 5m, exitPrice: 100m);

        // Coste = 5*100 + 1*(5/10) = 500.5 (mitad de la comisión del lote).
        Assert.Equal(500.5m, result.TotalCostBasis);
    }

    [Fact]
    public async Task EstimateAsync_SinPrecioIndicadoUsaElUltimoTick()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(OpenLot("AAA", 100m, 5m, BaseTime));
            ctx.MarketTicks.Add(MarketTick.Create("AAA", 90m, 0m, BaseTime.AddHours(1)));
            ctx.MarketTicks.Add(MarketTick.Create("AAA", 110m, 0m, BaseTime.AddHours(2)));   // el más reciente
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var result = await NewService(verify, sellFee: 0m).EstimateAsync("AAA", 5m);

        Assert.Equal(110m, result.ExitPriceUsed);
    }

    [Fact]
    public async Task EstimateAsync_SinPrecioNiTicks_UsaElPrecioDeEntradaDelPrimerLote()
    {
        await using (var ctx = NewContext())
        {
            ctx.Trades.Add(OpenLot("AAA", 100m, 5m, BaseTime));
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var result = await NewService(verify, sellFee: 0m).EstimateAsync("AAA", 5m);

        Assert.Equal(100m, result.ExitPriceUsed);
        Assert.Equal(0m, result.EstimatedGain);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task EstimateAsync_IncluyeLaDivisaDelSimbolo()
    {
        await using (var ctx = NewContext())
        {
            var tracked = TrackedSymbol.Create("AAA", "Test Inc.", BaseTime);
            tracked.SetCurrency("USD");
            ctx.TrackedSymbols.Add(tracked);
            ctx.Trades.Add(OpenLot("AAA", 100m, 5m, BaseTime));
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var result = await NewService(verify).EstimateAsync("AAA", 5m, exitPrice: 120m);

        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task EstimateAsync_SymbolVacio_Lanza()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.EstimateAsync("", 1m));
    }

    [Fact]
    public async Task EstimateAsync_CantidadInvalida_Lanza()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.EstimateAsync("AAA", 0m));
    }

    public void Dispose() => _connection.Dispose();
}

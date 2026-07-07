using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Application.Services;
using Comillas.AITradingSimulator.Domain.Enums;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Tests.Application;

public sealed class PositionServiceTests : IDisposable
{
    private static readonly DateTime BaseTime = new(2026, 6, 16, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TradingDbContext> _options;

    public PositionServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TradingDbContext>().UseSqlite(_connection).Options;
        using var ctx = new TradingDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    private TradingDbContext NewContext() => new(_options);

    // fee 0 por defecto para no alterar las expectativas de los tests previos (comisión = HV-050).
    private static PositionService NewService(TradingDbContext ctx, decimal fee = 0m)
        => new(ctx, new WatchlistService(ctx, TimeProvider.System), TimeProvider.System,
               Options.Create(new BrokerOptions { CommissionPerOrder = fee }));

    [Fact]
    public async Task OpenYClose_AplicaComisionPorOrden()
    {
        Guid id;
        await using (var ctx = NewContext())
            id = await NewService(ctx, 1m).OpenAsync("AAA", 100m, 10m, BaseTime);

        await using (var ctx = NewContext())
            Assert.Equal(1m, (await ctx.Trades.FirstAsync()).Commission);   // solo compra

        await using (var ctx = NewContext())
            await NewService(ctx, 1m).CloseAsync(id, 110m, BaseTime.AddHours(1));

        await using var v = NewContext();
        Assert.Equal(2m, (await v.Trades.FirstAsync()).Commission);     // compra + venta
    }

    [Fact]
    public async Task OpenAsync_CreaPosicionAbiertaYAnadeAWatchlist()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        var id = await sut.OpenAsync("hy9h.f", 1360m, 5m, BaseTime);

        await using var verify = NewContext();
        var trade = await verify.Trades.FindAsync(id);
        Assert.NotNull(trade);
        Assert.Equal("HY9H.F", trade!.Symbol);   // normalizado a mayúsculas
        Assert.Equal(1360m, trade.EntryPrice);
        Assert.Equal(5m, trade.Quantity);
        Assert.Equal(TradeStatus.Open, trade.Status);
        // Se añadió a la watchlist para obtener precio en vivo.
        Assert.True(await verify.TrackedSymbols.AnyAsync(t => t.Symbol == "HY9H.F"));
    }

    [Fact]
    public async Task OpenAsync_PermiteVariasPosicionesDelMismoSimbolo()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        await sut.OpenAsync("HY9H.F", 1360m, 5m, BaseTime);
        await sut.OpenAsync("HY9H.F", 1390m, 2m, BaseTime.AddDays(1));

        await using var verify = NewContext();
        Assert.Equal(2, await verify.Trades.CountAsync(t => t.Symbol == "HY9H.F" && t.Status == TradeStatus.Open));
        // El símbolo solo aparece una vez en la watchlist (no duplica).
        Assert.Equal(1, await verify.TrackedSymbols.CountAsync(t => t.Symbol == "HY9H.F"));
    }

    [Fact]
    public async Task OpenAsync_PrecioInvalido_Lanza()
    {
        await using var ctx = NewContext();
        var sut = NewService(ctx);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.OpenAsync("HY9H.F", 0m, 5m, BaseTime));
    }

    [Fact]
    public async Task CloseAsync_CierraLaPosicionYCalculaPnL()
    {
        Guid id;
        await using (var ctx = NewContext())
        {
            id = await NewService(ctx).OpenAsync("HY9H.F", 1360m, 5m, BaseTime);
        }

        await using var ctx2 = NewContext();
        var ok = await NewService(ctx2).CloseAsync(id, 1400m, BaseTime.AddHours(2));

        Assert.True(ok);
        await using var verify = NewContext();
        var trade = await verify.Trades.FindAsync(id);
        Assert.Equal(TradeStatus.Closed, trade!.Status);
        Assert.Equal(1400m, trade.ExitPrice);
        Assert.Equal((1400m - 1360m) * 5m, trade.RealizedPnL);
    }

    [Fact]
    public async Task CloseAsync_IdInexistente_DevuelveFalse()
    {
        await using var ctx = NewContext();
        Assert.False(await NewService(ctx).CloseAsync(Guid.NewGuid(), 100m, BaseTime));
    }

    [Fact]
    public async Task CloseAsync_YaCerrada_DevuelveFalse()
    {
        Guid id;
        await using (var ctx = NewContext())
        {
            var sut = NewService(ctx);
            id = await sut.OpenAsync("HY9H.F", 1360m, 5m, BaseTime);
            await sut.CloseAsync(id, 1400m, BaseTime.AddHours(1));
        }

        await using var ctx2 = NewContext();
        Assert.False(await NewService(ctx2).CloseAsync(id, 1500m, BaseTime.AddHours(2)));
    }

    [Fact]
    public async Task DeleteAsync_Existente_DevuelveTrue_NoExistente_False()
    {
        Guid id;
        await using (var ctx = NewContext())
        {
            id = await NewService(ctx).OpenAsync("HY9H.F", 1360m, 5m, BaseTime);
        }

        await using var ctx2 = NewContext();
        Assert.True(await NewService(ctx2).DeleteAsync(id));

        await using var ctx3 = NewContext();
        Assert.False(await NewService(ctx3).DeleteAsync(id));
        Assert.Equal(0, await ctx3.Trades.CountAsync());
    }

    [Fact]
    public async Task ImportCsvAsync_ConCabeceraYComaDecimalPunto_CreaPosiciones()
    {
        const string csv = "symbol,entry,qty,date\nHY9H.F,1360,5,2026-06-01\nAAPL,180.5,10,2026-05-20";

        await using var ctx = NewContext();
        var result = await NewService(ctx).ImportCsvAsync(csv);

        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Failed);

        await using var verify = NewContext();
        Assert.Equal(2, await verify.Trades.CountAsync(t => t.Status == TradeStatus.Open));
        var aapl = await verify.Trades.FirstAsync(t => t.Symbol == "AAPL");
        Assert.Equal(180.5m, aapl.EntryPrice);
        Assert.Equal(10m, aapl.Quantity);
        Assert.Equal(new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), aapl.CreatedAt);
    }

    [Fact]
    public async Task ImportCsvAsync_DelimitadorPuntoYComaConDecimalComa_CreaPosiciones()
    {
        // Excel español: delimitador ';' permite decimales con ','.
        const string csv = "símbolo;entry;qty\nHY9H.F;1360,5;5";

        await using var ctx = NewContext();
        var result = await NewService(ctx).ImportCsvAsync(csv);

        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.Failed);

        await using var verify = NewContext();
        var trade = await verify.Trades.FirstAsync(t => t.Symbol == "HY9H.F");
        Assert.Equal(1360.5m, trade.EntryPrice);
    }

    [Fact]
    public async Task ImportCsvAsync_FilaInvalida_NoAbortaElRestoYReportaError()
    {
        // 2ª línea (precio no numérico) falla; la 1ª y la 3ª se importan.
        const string csv = "HY9H.F,1360,5\nAAPL,abc,10\nGOOG,180,2";

        await using var ctx = NewContext();
        var result = await NewService(ctx).ImportCsvAsync(csv);

        Assert.Equal(2, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Errors);
        Assert.Equal(2, result.Errors[0].Line);   // línea 2 (1-based)

        await using var verify = NewContext();
        Assert.False(await verify.Trades.AnyAsync(t => t.Symbol == "AAPL"));
        Assert.True(await verify.Trades.AnyAsync(t => t.Symbol == "GOOG"));
    }

    [Fact]
    public async Task ImportCsvAsync_VacioOSoloCabecera_NoImportaNada()
    {
        await using var ctx = NewContext();
        var result = await NewService(ctx).ImportCsvAsync("symbol,entry,qty\n");

        Assert.Equal(0, result.Imported);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task ImportCsvAsync_ConExit_ImportaTradeCerrado()
    {
        // Con precio de salida (y fecha de cierre) la fila se importa como trade CERRADO (HV-047).
        const string csv = "symbol,entry,qty,date,exit,closeDate\nAAPL,180,10,2026-05-20,195,2026-06-10";

        await using var ctx = NewContext();
        var result = await NewService(ctx).ImportCsvAsync(csv);

        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.Failed);

        await using var verify = NewContext();
        var trade = await verify.Trades.FirstAsync(t => t.Symbol == "AAPL");
        Assert.Equal(TradeStatus.Closed, trade.Status);
        Assert.Equal(195m, trade.ExitPrice);
        Assert.Equal(150m, trade.RealizedPnL);   // (195 - 180) * 10
        Assert.Equal(new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc), trade.ClosedAt);
    }

    [Fact]
    public async Task ImportCsvAsync_ExitInvalido_ReportaError()
    {
        const string csv = "AAPL,180,10,2026-05-20,noesnumero";

        await using var ctx = NewContext();
        var result = await NewService(ctx).ImportCsvAsync(csv);

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Failed);

        await using var verify = NewContext();
        Assert.False(await verify.Trades.AnyAsync());
    }

    [Fact]
    public async Task ImportCsvAsync_CierreAnteriorApertura_ReportaErrorSinAbrir()
    {
        // La fecha de cierre no puede ser anterior a la de apertura → error, sin dejar la posición abierta.
        const string csv = "AAPL,180,10,2026-06-10,195,2026-06-01";

        await using var ctx = NewContext();
        var result = await NewService(ctx).ImportCsvAsync(csv);

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Failed);

        await using var verify = NewContext();
        Assert.Equal(0, await verify.Trades.CountAsync());   // ni siquiera abierta
    }

    public void Dispose() => _connection.Dispose();
}

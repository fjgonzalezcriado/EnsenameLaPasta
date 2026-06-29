using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.Persistence;

/// <summary>
/// Toma snapshots periódicos del valor de cuenta (y PnL) y los persiste como
/// <see cref="PortfolioSnapshot"/>, para graficar la evolución de la cuenta en el tiempo.
/// El histórico se construye hacia delante; con la app parada quedan huecos.
/// </summary>
public sealed class PortfolioSnapshotService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<SnapshotOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PortfolioSnapshotService> _logger;

    public PortfolioSnapshotService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<SnapshotOptions> options,
        TimeProvider timeProvider,
        ILogger<PortfolioSnapshotService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Margen tras el arranque (migraciones, primer feed de precios).
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
        catch (OperationCanceledException) { return; }

        if (_options.CurrentValue is { Enabled: true, SnapshotOnStartup: true })
            await TakeSnapshotSafelyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(30, _options.CurrentValue.IntervalSeconds));
            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }

            if (_options.CurrentValue.Enabled)
                await TakeSnapshotSafelyAsync(stoppingToken);
        }
    }

    private async Task TakeSnapshotSafelyAsync(CancellationToken ct)
    {
        try
        {
            await TakeSnapshotAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutdown en curso
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tomando snapshot del valor de cuenta.");
        }
    }

    /// <summary>Computa y persiste un snapshot del valor de cuenta ahora mismo (seam de pruebas).</summary>
    public async Task TakeSnapshotAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        // priceSeriesPoints mínimo: no necesitamos la serie de precios para el snapshot.
        var snap = await dashboard.GetSnapshotAsync(priceSeriesPoints: 10, cancellationToken: ct);

        // El valor de cuenta puede ser negativo en casos límite (retiradas > saldo);
        // la entidad no admite capital negativo, así que en ese caso omitimos el punto.
        if (snap.AccountValue < 0m)
        {
            _logger.LogWarning(
                "Valor de cuenta negativo ({Value}); se omite el snapshot.", snap.AccountValue);
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var entity = PortfolioSnapshot.Create(
            now, snap.AccountValue, snap.RealizedPnL, snap.UnrealizedPnL,
            snap.OpenPositions, snap.ClosedTrades);

        db.PortfolioSnapshots.Add(entity);
        await db.SaveChangesAsync(ct);
    }
}

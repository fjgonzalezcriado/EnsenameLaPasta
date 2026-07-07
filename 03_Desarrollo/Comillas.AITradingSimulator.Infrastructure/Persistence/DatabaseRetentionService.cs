using Comillas.AITradingSimulator.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.Persistence;

/// <summary>
/// Mantiene el tamaño de la BD SQLite bajo un límite configurable: cuando el
/// fichero supera el máximo, purga los <see cref="Domain.Entities.MarketTick"/>
/// más antiguos hasta bajar a la marca de agua baja y compacta con VACUUM
/// (borrar filas no reduce el fichero en SQLite sin VACUUM).
/// </summary>
public sealed class DatabaseRetentionService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<RetentionOptions> options,
    ILogger<DatabaseRetentionService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IOptionsMonitor<RetentionOptions> _options = options;
    private readonly ILogger<DatabaseRetentionService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Pequeño margen tras el arranque (migraciones, etc.).
        try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            var opts = _options.CurrentValue;

            if (opts.Enabled)
            {
                try
                {
                    await EnforceLimitAsync(opts, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error aplicando la retención de la BD.");
                }
            }

            var interval = TimeSpan.FromSeconds(Math.Max(30, opts.CheckIntervalSeconds));
            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task EnforceLimitAsync(RetentionOptions opts, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        var maxBytes = (long)opts.MaxDatabaseSizeMb * 1024L * 1024L;
        if (maxBytes <= 0) return;

        var currentBytes = await db.GetDatabaseSizeBytesAsync(ct);
        if (currentBytes <= maxBytes) return;

        var total = await db.MarketTicks.CountAsync(ct);
        if (total == 0)
        {
            _logger.LogWarning(
                "BD a {SizeMB} MB supera el límite ({LimitMB} MB) pero no hay ticks que purgar (el peso es de otras tablas).",
                currentBytes / (1024 * 1024), opts.MaxDatabaseSizeMb);
            return;
        }

        // Asumimos que los MarketTick dominan el tamaño: borramos la fracción
        // necesaria para acercarnos a la marca de agua baja.
        var lowWater = Math.Clamp(opts.LowWaterMarkFraction, 0.1, 0.95);
        var targetBytes = maxBytes * lowWater;
        var deleteFraction = Math.Clamp(1.0 - (targetBytes / currentBytes), 0.01, 0.95);
        var deleteCount = (int)Math.Ceiling(total * deleteFraction);
        if (deleteCount <= 0) return;

        // Timestamp de corte: el del tick en la posición deleteCount (ascendente).
        var cutoff = await db.MarketTicks
            .OrderBy(t => t.Timestamp)
            .Skip(deleteCount - 1)
            .Select(t => (DateTime?)t.Timestamp)
            .FirstOrDefaultAsync(ct);
        if (cutoff is null) return;

        var deleted = await db.MarketTicks
            .Where(t => t.Timestamp <= cutoff.Value)
            .ExecuteDeleteAsync(ct);

        // VACUUM: devuelve al SO el espacio liberado (SQLite no lo hace solo).
        await db.Database.ExecuteSqlRawAsync("VACUUM;", ct);

        var newBytes = await db.GetDatabaseSizeBytesAsync(ct);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Retención: BD {OldMB:F1} MB > {LimitMB} MB → purgados {Deleted} ticks (<= {Cutoff:u}); compactada a {NewMB:F1} MB.",
                currentBytes / 1048576.0, opts.MaxDatabaseSizeMb, deleted, cutoff.Value, newBytes / 1048576.0);
    }
}

using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Mantiene calientes en caché los tipos de cambio de las divisas en uso (las de la
/// watchlist y las de los movimientos de caja), refrescándolos cada
/// <see cref="FxOptions.RefreshSeconds"/>. Así el dashboard nunca hace la llamada HTTP
/// a la fuente FX en el hot path: siempre lee de una caché ya poblada (HV-023).
/// </summary>
public sealed class FxRefreshService(
    IServiceScopeFactory scopeFactory,
    IFxRateProvider fx,
    IOptionsMonitor<FxOptions> options,
    ILogger<FxRefreshService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IFxRateProvider _fx = fx;
    private readonly IOptionsMonitor<FxOptions> _options = options;
    private readonly ILogger<FxRefreshService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Margen tras el arranque; el primer refresco calienta la caché antes de que
        // el dashboard la necesite (si llega antes, GetRateAsync hace el fetch perezoso).
        try { await Task.Delay(TimeSpan.FromSeconds(12), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshInUseAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refrescando los tipos de cambio en background.");
            }

            var interval = TimeSpan.FromSeconds(Math.Max(30, _options.CurrentValue.RefreshSeconds));
            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>Refresca los tipos de las divisas en uso hacia la base (seam de pruebas).</summary>
    public async Task RefreshInUseAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        var baseCurrency = (_options.CurrentValue.BaseCurrency ?? "EUR").Trim().ToUpperInvariant();

        var symbolCcys = await db.TrackedSymbols.Select(t => t.Currency).ToListAsync(ct);
        var cashCcys = await db.CashMovements.Select(m => m.Currency).ToListAsync(ct);

        var currencies = symbolCcys.Concat(cashCcys)
            .Select(c => (c ?? string.Empty).Trim().ToUpperInvariant())
            .Where(c => c.Length > 0 && c != baseCurrency)
            .Distinct()
            .ToList();

        foreach (var ccy in currencies)
            await _fx.RefreshAsync(ccy, baseCurrency, ct);

        if (currencies.Count > 0 && _logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("FX refrescados {Count} tipos hacia {Base}.", currencies.Count, baseCurrency);
    }
}

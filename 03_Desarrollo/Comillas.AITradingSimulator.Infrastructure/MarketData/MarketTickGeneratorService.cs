using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

public sealed class MarketTickGeneratorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMarketDataProvider _provider;
    private readonly ITickBus _bus;
    private readonly IOptionsMonitor<MarketDataOptions> _optionsMonitor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MarketTickGeneratorService> _logger;

    public MarketTickGeneratorService(
        IServiceScopeFactory scopeFactory,
        IMarketDataProvider provider,
        ITickBus bus,
        IOptionsMonitor<MarketDataOptions> optionsMonitor,
        TimeProvider timeProvider,
        ILogger<MarketTickGeneratorService> logger)
    {
        _scopeFactory = scopeFactory;
        _provider = provider;
        _bus = bus;
        _optionsMonitor = optionsMonitor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMs = _optionsMonitor.CurrentValue.TickIntervalMs;

        _logger.LogInformation(
            "Iniciando generador de ticks (datos reales). Intervalo: {Interval}ms. Símbolos: watchlist (BD).",
            intervalMs);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(intervalMs),
            _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await GenerateAndPersistAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Generador de ticks detenido por cancelación.");
        }
    }

    private async Task GenerateAndPersistAsync(CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        // Símbolos seguidos: leídos de la BD en cada ciclo (entidades, para poder
        // sellar la divisa). Así las altas/bajas desde el buscador se reflejan sin reiniciar.
        var tracked = await db.TrackedSymbols.ToListAsync(ct);
        if (tracked.Count == 0) return;

        // Pedimos el precio real de cada símbolo al proveedor. Capturamos errores
        // por símbolo individual para no romper el ciclo si Yahoo falla para uno.
        var batch = new List<MarketTick>(tracked.Count);
        foreach (var item in tracked)
        {
            try
            {
                var quote = await _provider.GetLatestAsync(item.Symbol, now, ct);
                batch.Add(quote.Tick);

                // Sella la divisa de cotización si aún no se conoce o cambió.
                if (!string.IsNullOrWhiteSpace(quote.Currency)
                    && !string.Equals(item.Currency, quote.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    item.SetCurrency(quote.Currency!);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Error obteniendo tick para {Symbol}, se omite este ciclo.", item.Symbol);
            }
        }

        if (batch.Count == 0) return;

        foreach (var tick in batch)
        {
            db.MarketTicks.Add(tick);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error persistiendo ticks de mercado.");
            return;  // sin persistir, no publicamos al bus
        }

        // Publicar al bus tras persistencia exitosa
        foreach (var tick in batch)
        {
            try
            {
                await _bus.PublishAsync(tick, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error publicando tick al bus.");
            }
        }
    }
}

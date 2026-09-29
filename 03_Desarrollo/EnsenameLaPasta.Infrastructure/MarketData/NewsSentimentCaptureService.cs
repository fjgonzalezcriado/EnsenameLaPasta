using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Application.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Infrastructure.MarketData;

/// <summary>
/// Recorre periódicamente la watchlist y captura el sentimiento diario de noticias de cada
/// símbolo (HV-056), sentando la base de histórico para un futuro uso como feature del
/// clasificador ML. Por sí sola esta captura NO cambia ninguna predicción ni señal existente.
/// </summary>
public sealed class NewsSentimentCaptureService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<NewsSentimentOptions> options,
    ILogger<NewsSentimentCaptureService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IOptionsMonitor<NewsSentimentOptions> _options = options;
    private readonly ILogger<NewsSentimentCaptureService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Margen tras el arranque: deja que la watchlist y el resto de servicios estén listos.
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        if (_options.CurrentValue is { Enabled: true, CaptureOnStartup: true })
            await CaptureAllSafelyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(300, _options.CurrentValue.IntervalSeconds));
            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }

            if (_options.CurrentValue.Enabled)
                await CaptureAllSafelyAsync(stoppingToken);
        }
    }

    private async Task CaptureAllSafelyAsync(CancellationToken ct)
    {
        try
        {
            await CaptureAllAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutdown en curso
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturando sentimiento de noticias.");
        }
    }

    /// <summary>Captura el sentimiento de hoy para toda la watchlist, símbolo a símbolo (seam de pruebas).</summary>
    public async Task CaptureAllAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var watchlist = scope.ServiceProvider.GetRequiredService<IWatchlistService>();
        var sentiment = scope.ServiceProvider.GetRequiredService<INewsSentimentService>();

        var symbols = (await watchlist.GetAllAsync(ct)).Select(s => s.Symbol).ToList();
        var delay = TimeSpan.FromMilliseconds(Math.Max(0, _options.CurrentValue.DelayBetweenSymbolsMs));
        var captured = 0;

        foreach (var symbol in symbols)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var result = await sentiment.CaptureTodayAsync(symbol, ct);
                if (result is not null) captured++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sentimiento de noticias falló para {Symbol}.", symbol);
            }

            if (delay > TimeSpan.Zero)
            {
                try { await Task.Delay(delay, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Sentimiento de noticias: {Captured}/{Total} símbolos con datos hoy.", captured, symbols.Count);
    }
}

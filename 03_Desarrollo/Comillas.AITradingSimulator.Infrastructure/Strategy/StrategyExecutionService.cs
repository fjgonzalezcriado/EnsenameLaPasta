using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Application.Strategies;
using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.Strategy;

public sealed class StrategyExecutionService : BackgroundService
{
    private readonly ITickBus _bus;
    private readonly IStrategy _strategy;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<StrategyOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<StrategyExecutionService> _logger;

    public StrategyExecutionService(
        ITickBus bus,
        IStrategy strategy,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<StrategyOptions> options,
        TimeProvider timeProvider,
        ILogger<StrategyExecutionService> logger)
    {
        _bus = bus;
        _strategy = strategy;
        _scopeFactory = scopeFactory;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.CurrentValue;
        _logger.LogInformation(
            "Iniciando estrategia MA Crossover. ShortWindow={Short} LongWindow={Long} Quantity={Qty}",
            opts.ShortWindow, opts.LongWindow, opts.Quantity);

        try
        {
            await foreach (var tick in _bus.SubscribeAsync(stoppingToken))
            {
                var signal = _strategy.OnTick(tick);
                if (signal is null) continue;

                await ExecuteSignalAsync(signal.Value, tick, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Estrategia detenida por cancelación.");
        }
    }

    private async Task ExecuteSignalAsync(TradeSignal signal, MarketTick tick, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            Trade? result;
            if (signal == TradeSignal.Buy)
            {
                result = await orders.BuyAsync(tick.Symbol, tick.Price, _options.CurrentValue.Quantity, now, ct);
                if (result is not null)
                {
                    _logger.LogInformation(
                        "BUY {Symbol} @ {Price} qty={Qty}",
                        result.Symbol, result.EntryPrice, result.Quantity);
                }
            }
            else
            {
                result = await orders.CloseAsync(tick.Symbol, tick.Price, now, ct);
                if (result is not null)
                {
                    _logger.LogInformation(
                        "SELL {Symbol} @ {Price} pnl={PnL}",
                        result.Symbol, result.ExitPrice, result.RealizedPnL);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error ejecutando señal {Signal} para {Symbol}", signal, tick.Symbol);
        }
    }
}

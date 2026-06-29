using Comillas.AITradingSimulator.Domain.Entities;
using Comillas.AITradingSimulator.Infrastructure.MarketData;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class ChannelTickBusTests
{
    private static readonly DateTime BaseTime = new(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PublishYSubscribe_TickEntregadoAlSubscriber()
    {
        var bus = new ChannelTickBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Subscriber task
        var subscriberTask = Task.Run(async () =>
        {
            await foreach (var tick in bus.SubscribeAsync(cts.Token))
            {
                return tick;
            }
            return null;
        });

        // Publish
        var published = MarketTick.Create("AAPL", 150m, 100m, BaseTime);
        await bus.PublishAsync(published, cts.Token);

        var received = await subscriberTask;

        Assert.NotNull(received);
        Assert.Equal("AAPL", received!.Symbol);
        Assert.Equal(150m, received.Price);
    }

    [Fact]
    public async Task Subscribe_ConCancellation_TerminaCorrectamente()
    {
        var bus = new ChannelTickBus();
        using var cts = new CancellationTokenSource();

        var subscriberTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var _ in bus.SubscribeAsync(cts.Token))
                {
                    // sin datos, no debería iterar
                }
                return "completed";
            }
            catch (OperationCanceledException)
            {
                return "cancelled";
            }
        });

        // Cancelar al instante
        await Task.Delay(50);
        cts.Cancel();

        var outcome = await subscriberTask;
        Assert.Contains(outcome, new[] { "cancelled", "completed" });
    }

    [Fact]
    public async Task Publish_MultipleTicks_SubscriberRecibeEnOrden()
    {
        var bus = new ChannelTickBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var received = new List<MarketTick>();
        var subscriberTask = Task.Run(async () =>
        {
            await foreach (var tick in bus.SubscribeAsync(cts.Token))
            {
                received.Add(tick);
                if (received.Count == 3) break;
            }
        });

        await bus.PublishAsync(MarketTick.Create("AAPL", 100m, 100m, BaseTime), cts.Token);
        await bus.PublishAsync(MarketTick.Create("GOOG", 200m, 200m, BaseTime), cts.Token);
        await bus.PublishAsync(MarketTick.Create("BTCUSD", 65000m, 1m, BaseTime), cts.Token);

        await subscriberTask;

        Assert.Equal(3, received.Count);
        Assert.Equal("AAPL", received[0].Symbol);
        Assert.Equal("GOOG", received[1].Symbol);
        Assert.Equal("BTCUSD", received[2].Symbol);
    }
}

using Comillas.AITradingSimulator.Domain.Entities;

namespace Comillas.AITradingSimulator.Application.Common.Interfaces;

public interface ITickBus
{
    ValueTask PublishAsync(MarketTick tick, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MarketTick> SubscribeAsync(CancellationToken cancellationToken = default);
}

using EnsenameLaPasta.Domain.Entities;

namespace EnsenameLaPasta.Application.Common.Interfaces;

public interface ITickBus
{
    ValueTask PublishAsync(MarketTick tick, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MarketTick> SubscribeAsync(CancellationToken cancellationToken = default);
}

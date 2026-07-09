using System.Threading.Channels;
using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Domain.Entities;

namespace EnsenameLaPasta.Infrastructure.MarketData;

public sealed class ChannelTickBus : ITickBus
{
    private readonly Channel<MarketTick> _channel = Channel.CreateBounded<MarketTick>(
        new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask PublishAsync(MarketTick tick, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(tick, cancellationToken);

    public IAsyncEnumerable<MarketTick> SubscribeAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}

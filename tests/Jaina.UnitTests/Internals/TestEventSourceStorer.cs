using Jaina.EventBus;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Jaina.UnitTests;

public class TestEventSourceStorer : IEventSourceStorer
{
    private readonly Channel<IEventSource> _channel;

    public TestEventSourceStorer(int capacity)
    {
        var boundedChannelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _channel = Channel.CreateBounded<IEventSource>(boundedChannelOptions);
    }

    public async ValueTask WriteAsync(IEventSource eventSource, CancellationToken cancellationToken)
    {
        if (eventSource == default)
        {
            throw new ArgumentNullException(nameof(eventSource));
        }

        await _channel.Writer.WriteAsync(eventSource, cancellationToken);
    }

    public async ValueTask<IEventSource> ReadAsync(CancellationToken cancellationToken)
    {
        var eventSource = await _channel.Reader.ReadAsync(cancellationToken);
        return eventSource;
    }
}
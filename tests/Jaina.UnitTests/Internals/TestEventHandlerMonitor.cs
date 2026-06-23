using System.Threading;
using System.Threading.Tasks;

namespace Jaina.UnitTests;

internal class TestEventHandlerMonitor : IEventHandlerMonitor
{
    public Task OnExecutedAsync(EventHandlerExecutedContext context, CancellationToken cancellationToken)
    {
        ThreadStaticValue.MonitorValue += 1;
        return Task.CompletedTask;
    }

    public Task OnExecutingAsync(EventHandlerExecutingContext context, CancellationToken cancellationToken)
    {
        ThreadStaticValue.MonitorValue += 1;
        return Task.CompletedTask;
    }
}
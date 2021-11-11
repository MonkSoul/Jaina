using Jaina.EventBus;
using System.Threading.Tasks;

namespace Jaina.UnitTests;

internal class TestEventHandlerMonitor : IEventHandlerMonitor
{
    public Task OnExecutedAsync(EventHandlerExecutedContext context)
    {
        ThreadStaticValue.MonitorValue += 1;
        return Task.CompletedTask;
    }

    public Task OnExecutingAsync(EventHandlerExecutingContext context)
    {
        ThreadStaticValue.MonitorValue += 1;
        return Task.CompletedTask;
    }
}

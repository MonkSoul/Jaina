using Jaina.EventBus;
using System;
using System.Threading.Tasks;

namespace Jaina.UnitTests;

internal class TestEventHandlerExecutor : IEventHandlerExecutor
{
    public async Task ExecuteAsync(EventHandlerExecutingContext context, Func<EventHandlerExecutingContext, Task> handler)
    {
        ThreadStaticValue.ExecutorValue += 1;
        await handler(context);
        ThreadStaticValue.ExecutorValue += 1;
    }
}

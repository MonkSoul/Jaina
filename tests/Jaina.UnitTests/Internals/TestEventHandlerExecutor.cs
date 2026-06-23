using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jaina.UnitTests;

internal class TestEventHandlerExecutor : IEventHandlerExecutor
{
    public async Task ExecuteAsync(EventHandlerExecutingContext context, Func<EventHandlerExecutingContext, Task> handler, CancellationToken cancellationToken)
    {
        ThreadStaticValue.ExecutorValue += 1;
        await handler(context);
        ThreadStaticValue.ExecutorValue += 1;
    }
}
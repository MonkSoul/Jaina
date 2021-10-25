using Jaina.EventBus;
using System;
using System.Threading.Tasks;

namespace Jaina.UnitTests
{
    internal class TestEventHandlerExecutor : IEventHandlerExecutor
    {
        public Task ExecuteAsync(EventHandlerExecutingContext context, Func<EventHandlerExecutingContext, Task> handler)
        {
            return handler(context);
        }
    }
}
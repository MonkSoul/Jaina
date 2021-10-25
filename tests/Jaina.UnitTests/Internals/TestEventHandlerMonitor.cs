using Jaina.EventBus;
using System.Threading.Tasks;

namespace Jaina.UnitTests
{
    internal class TestEventHandlerMonitor : IEventHandlerMonitor
    {
        public Task OnExecutedAsync(EventHandlerExecutedContext context)
        {
            return Task.CompletedTask;
        }

        public Task OnExecutingAsync(EventHandlerExecutingContext context)
        {
            return Task.CompletedTask;
        }
    }
}
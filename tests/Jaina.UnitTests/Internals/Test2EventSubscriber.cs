using Jaina.EventBus;
using System.Threading.Tasks;

namespace Jaina.UnitTests
{
    internal class Test2EventSubscriber : IEventSubscriber
    {
        [EventSubscribe("Unit2:Test")]
        public Task CreateTest(EventHandlerExecutingContext context)
        {
            return Task.CompletedTask;
        }
    }
}
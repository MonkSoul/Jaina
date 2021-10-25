using Jaina.EventBus;
using System.Threading.Tasks;

namespace Jaina.UnitTests
{
    internal class TestEventSubscriber : IEventSubscriber
    {
        [EventSubscribe("Unit:Test")]
        public Task CreateTest(EventHandlerExecutingContext context)
        {
            return Task.CompletedTask;
        }

        [EventSubscribe("Unit:Test2")]
        [EventSubscribe("Unit:Test3")]
        public Task CreateTest2(EventHandlerExecutingContext context)
        {
            return Task.CompletedTask;
        }
    }
}
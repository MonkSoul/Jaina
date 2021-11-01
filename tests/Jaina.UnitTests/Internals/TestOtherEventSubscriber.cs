using Jaina.EventBus;
using System.Threading.Tasks;

namespace Jaina.UnitTests
{
    public class TestOtherEventSubscriber : IEventSubscriber
    {
        [EventSubscribe("Unit:Other:Test")]
        public Task CreateTest(EventHandlerExecutingContext context)
        {
            return Task.CompletedTask;
        }
    }
}
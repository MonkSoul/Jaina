using Jaina.EventBus;
using System;
using System.Threading.Tasks;

namespace Jaina.UnitTests
{
    internal class TestEventSubscriber : IEventSubscriber
    {
        private Object obj = new();

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

        [EventSubscribe("Unit:Publisher")]
        public Task TestPublisher(EventHandlerExecutingContext context)
        {
            lock (obj)
            {
                var i = int.Parse(context.Source.Payload.ToString());
                ThreadStaticValue.PublishValue += i;
            }
            return Task.CompletedTask;
        }
    }
}
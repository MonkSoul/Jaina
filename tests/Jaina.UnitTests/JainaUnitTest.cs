using FluentAssertions;
using Jaina.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Jaina.UnitTests
{
    public class JainaUnitTest
    {
        [Fact]
        public void TestDefaultEventBus()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddEventBus();

                services.Any(s => s.ServiceType == typeof(IEventSourceStorer) && s.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
                services.Any(s => s.ServiceType == typeof(IEventPublisher) && s.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
                services.Any(s => s.ServiceType == typeof(IEventSubscriber) && s.Lifetime == ServiceLifetime.Singleton).Should().BeFalse();
            });

            var app = builder.Build();
            var services = app.Services;

            var eventBusHostedService = services.GetService<IHostedService>();
            eventBusHostedService.GetType().Name.Should().Be("EventBusHostedService");
        }

        [Fact]
        public void TestEventBusOptionsBuilder()
        {
            var eventBusOptionsBuilder = new EventBusOptionsBuilder();
            eventBusOptionsBuilder.ChannelCapacity.Should().Be(3000);
            eventBusOptionsBuilder.UnobservedTaskExceptionHandler.Should().Be(default);

            var builderType = typeof(EventBusOptionsBuilder);
            builderType.Invoking(t => t.GetField("_eventSubscribers").GetValue(eventBusOptionsBuilder).Should().NotBeNull());
            builderType.Invoking(t => t.GetField("_eventPublisher").GetValue(eventBusOptionsBuilder).Should().BeNull());
            builderType.Invoking(t => t.GetField("_eventSourceStorerImplementationFactory").GetValue(eventBusOptionsBuilder).Should().BeNull());
            builderType.Invoking(t => t.GetField("_eventHandlerMonitor").GetValue(eventBusOptionsBuilder).Should().BeNull());
            builderType.Invoking(t => t.GetField("_eventHandlerExecutor").GetValue(eventBusOptionsBuilder).Should().BeNull());
        }

        [Fact]
        public void TestSubscriber()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddEventBus(builder =>
                {
                    builder.AddSubscriber<TestEventSubscriber>();
                });

                services.Any(s => s.ServiceType == typeof(IEventSubscriber) && s.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
            });

            var app = builder.Build();
            var services = app.Services;

            var eventBusHostedService = services.GetService<IHostedService>();
            var eventBusHostedType = eventBusHostedService.GetType();
            eventBusHostedType.Invoking(t =>
            {
                var eventHandlersField = t.GetField("_eventHandlers");
                var eventHandlers = eventHandlersField.GetValue(eventBusHostedType);

                eventHandlers.Should().NotBeNull();
                eventHandlersField.FieldType.IsGenericType.Should().BeTrue();

                var _hashSetType = eventHandlers.GetType();
                _hashSetType.Invoking(t => t.GetProperty("Count").GetValue(eventHandlers).ToString().Should().Be("3"));
            });
        }

        [Fact]
        public void TestMultipleSubscriber()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddEventBus(builder =>
                {
                    builder.AddSubscriber<TestEventSubscriber>();
                    builder.AddSubscriber<Test2EventSubscriber>();
                });

                services.Count(s => s.ServiceType == typeof(IEventSubscriber) && s.Lifetime == ServiceLifetime.Singleton).Should().Be(2);
            });

            var app = builder.Build();
            var services = app.Services;

            var subscribers = services.GetServices<IEventSubscriber>();
            subscribers.Count().Should().Be(2);

            var eventBusHostedService = services.GetService<IHostedService>();
            var eventBusHostedType = eventBusHostedService.GetType();
            eventBusHostedType.Invoking(t =>
            {
                var eventHandlersField = t.GetField("_eventHandlers");
                var eventHandlers = eventHandlersField.GetValue(eventBusHostedType);

                var _hashSetType = eventHandlers.GetType();
                _hashSetType.Invoking(t => t.GetProperty("Count").GetValue(eventHandlers).ToString().Should().Be("5"));
            });
        }

        [Fact]
        public void TestMonitor()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.Any(s => s.ServiceType == typeof(IEventHandlerMonitor) && s.Lifetime == ServiceLifetime.Singleton).Should().BeFalse();

                services.AddEventBus(builder =>
                {
                    builder.AddMonitor<TestEventHandlerMonitor>();
                });

                services.Any(s => s.ServiceType == typeof(IEventHandlerMonitor) && s.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
            });

            var app = builder.Build();
            var services = app.Services;

            var monitor = services.GetService<IEventHandlerMonitor>();
            monitor.Should().NotBeNull();
        }

        [Fact]
        public void TestExecutor()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.Any(s => s.ServiceType == typeof(IEventHandlerExecutor) && s.Lifetime == ServiceLifetime.Singleton).Should().BeFalse();

                services.AddEventBus(builder =>
                {
                    builder.AddExecutor<TestEventHandlerExecutor>();
                });

                services.Any(s => s.ServiceType == typeof(IEventHandlerExecutor) && s.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
            });

            var app = builder.Build();
            var services = app.Services;

            var monitor = services.GetService<IEventHandlerExecutor>();
            monitor.Should().NotBeNull();
        }

        [Fact]
        public async Task TestPublisher()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddEventBus(builder =>
                {
                    builder.AddSubscriber<TestEventSubscriber>();
                });
            });

            var app = builder.Build();
            var services = app.Services;

            var eventBusHostedService = services.GetService<IHostedService>() as BackgroundService;
            var cancellationTokenSource = new CancellationTokenSource();
            await eventBusHostedService.StartAsync(cancellationTokenSource.Token);

            var eventPublisher = services.GetService<IEventPublisher>();

            for (int i = 1; i <= 10; i++)
            {
                var eventSource = new ChannelEventSource("Unit:Publisher", 1);
                await eventPublisher.PublishAsync(eventSource);
            }

            await Task.Delay(1000);

            ThreadStaticValue.Value.Should().Be(11);

            await eventBusHostedService.StopAsync(cancellationTokenSource.Token);
        }
    }
}
using FluentAssertions;
using Jaina.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
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
    }
}
using FluentAssertions;
using Jaina.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Jaina.UnitTests;

public class JainaUnitTest
{
    protected readonly ITestOutputHelper Output;

    public JainaUnitTest(ITestOutputHelper tempOutput)
    {
        Output = tempOutput;
    }

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
        var bindingAttr = BindingFlags.NonPublic | BindingFlags.Instance;
        builderType.Invoking(t => t.GetField("_eventSubscribers", bindingAttr).GetValue(eventBusOptionsBuilder).Should().NotBeNull()).Should().NotThrow();
        builderType.Invoking(t => t.GetField("_eventPublisher", bindingAttr).GetValue(eventBusOptionsBuilder).Should().BeNull()).Should().NotThrow();
        builderType.Invoking(t => t.GetField("_eventSourceStorerImplementationFactory", bindingAttr).GetValue(eventBusOptionsBuilder).Should().BeNull()).Should().NotThrow();
        builderType.Invoking(t => t.GetField("_eventHandlerMonitor", bindingAttr).GetValue(eventBusOptionsBuilder).Should().BeNull()).Should().NotThrow();
        builderType.Invoking(t => t.GetField("_eventHandlerExecutor", bindingAttr).GetValue(eventBusOptionsBuilder).Should().BeNull()).Should().NotThrow();
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
            var bindingAttr = BindingFlags.NonPublic | BindingFlags.Instance;
            var eventHandlersField = t.GetField("_eventHandlers", bindingAttr);
            var eventHandlers = eventHandlersField.GetValue(eventBusHostedService);

            eventHandlers.Should().NotBeNull();
            eventHandlersField.FieldType.IsGenericType.Should().BeTrue();

            var _hashSetType = eventHandlers.GetType();
            _hashSetType.Invoking(t => t.GetProperty("Count").GetValue(eventHandlers).ToString().Should().Be("4"));
        }).Should().NotThrow();
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
                builder.AddSubscriber<TestOtherEventSubscriber>();
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
            var bindingAttr = BindingFlags.NonPublic | BindingFlags.Instance;
            var eventHandlersField = t.GetField("_eventHandlers", bindingAttr);
            var eventHandlers = eventHandlersField.GetValue(eventBusHostedService);

            var _hashSetType = eventHandlers.GetType();
            _hashSetType.Invoking(t => t.GetProperty("Count").GetValue(eventHandlers).ToString().Should().Be("6")).Should().NotThrow();
        }).Should().NotThrow();
    }

    [Fact]
    public async Task TestMonitor()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.Any(s => s.ServiceType == typeof(IEventHandlerMonitor) && s.Lifetime == ServiceLifetime.Singleton).Should().BeFalse();

            services.AddEventBus(builder =>
            {
                builder.AddSubscriber<TestEventSubscriber>();
                builder.AddMonitor<TestEventHandlerMonitor>();
            });

            services.Any(s => s.ServiceType == typeof(IEventHandlerMonitor) && s.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
        });

        var app = builder.Build();
        var services = app.Services;

        var monitor = services.GetService<IEventHandlerMonitor>();
        monitor.Should().NotBeNull();

        var eventBusHostedService = services.GetService<IHostedService>() as BackgroundService;
        var cancellationTokenSource = new CancellationTokenSource();
        await eventBusHostedService.StartAsync(cancellationTokenSource.Token);

        var eventPublisher = services.GetService<IEventPublisher>();
        var eventSource = new ChannelEventSource("Unit:Publisher", 1);
        await eventPublisher.PublishAsync(eventSource);

        await Task.Delay(1000);

        ThreadStaticValue.MonitorValue.Should().Be(3);

        await eventBusHostedService.StopAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task TestExecutor()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.Any(s => s.ServiceType == typeof(IEventHandlerExecutor) && s.Lifetime == ServiceLifetime.Singleton).Should().BeFalse();

            services.AddEventBus(builder =>
            {
                builder.AddSubscriber<TestEventSubscriber>();
                builder.AddExecutor<TestEventHandlerExecutor>();
            });

            services.Any(s => s.ServiceType == typeof(IEventHandlerExecutor) && s.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
        });

        var app = builder.Build();
        var services = app.Services;

        var monitor = services.GetService<IEventHandlerExecutor>();
        monitor.Should().NotBeNull();

        var eventBusHostedService = services.GetService<IHostedService>() as BackgroundService;
        var cancellationTokenSource = new CancellationTokenSource();
        await eventBusHostedService.StartAsync(cancellationTokenSource.Token);

        var eventPublisher = services.GetService<IEventPublisher>();
        var eventSource = new ChannelEventSource("Unit:Publisher", 1);
        await eventPublisher.PublishAsync(eventSource);

        await Task.Delay(1000);

        ThreadStaticValue.ExecutorValue.Should().Be(3);

        await eventBusHostedService.StopAsync(cancellationTokenSource.Token);
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
        ThreadStaticValue.PublishValue = 1;

        for (int i = 1; i <= 10; i++)
        {
            var eventSource = new ChannelEventSource("Unit:Publisher", 1);
            await eventPublisher.PublishAsync(eventSource);
        }

        await Task.Delay(1000);

        ThreadStaticValue.PublishValue.Should().Be(11);

        await eventBusHostedService.StopAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task TestPublisherEnum()
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
        ThreadStaticValue.PulishValueEnum = 1;

        await eventPublisher.PublishAsync(TestEnum.Message, 1);

        await Task.Delay(1000);

        ThreadStaticValue.PulishValueEnum.Should().Be(1);

        await eventBusHostedService.StopAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task TestCancel()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddEventBus(builder =>
            {
                builder.AddSubscriber<TestEventSubscriber>();

                builder.UnobservedTaskExceptionHandler = (obj, args) =>
                {
                    Output.WriteLine(args.Exception.InnerException.Message);
                    args.Exception.InnerException.GetType().Should().Be(typeof(OperationCanceledException));
                };
            });
        });

        var app = builder.Build();
        var services = app.Services;

        var eventBusHostedService = services.GetService<IHostedService>() as BackgroundService;
        var cancellationTokenSource = new CancellationTokenSource();
        await eventBusHostedService.StartAsync(cancellationTokenSource.Token);

        var eventPublisher = services.GetService<IEventPublisher>();

        var cts = new CancellationTokenSource();
        var eventSource = new ChannelEventSource("Unit:Publisher", 1, cts.Token);
        await eventPublisher.PublishAsync(eventSource);
        cts.Cancel();

        await Task.Delay(1000);

        await eventBusHostedService.StopAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public void TestBatchRegisteSubscribers()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddEventBus(builder =>
            {
                builder.AddSubscribers(this.GetType().Assembly);
            });

            services.Count(s => s.ServiceType == typeof(IEventSubscriber) && s.Lifetime == ServiceLifetime.Singleton).Should().Be(2);
        });

        var app = builder.Build();
        var services = app.Services;
    }

    [Fact]
    public void TestCustomStorer()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddEventBus(builder =>
            {
                builder.AddSubscriber<TestEventSubscriber>();
                builder.ReplaceStorer(serviceProvider =>
                {
                    return new TestEventSourceStorer(100);
                });
            });
        });

        var app = builder.Build();
        var services = app.Services;

        var eventSourceStorer = services.GetRequiredService<IEventSourceStorer>();
        eventSourceStorer.GetType().Name.Should().Be(nameof(TestEventSourceStorer));
    }
}
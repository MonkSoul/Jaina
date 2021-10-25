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

                // 测试默认服务
                services.Any(s => s.ServiceType == typeof(IEventSourceStorer) && s.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
                services.Any(s => s.ServiceType == typeof(IEventPublisher) && s.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
            });

            // 测试默认服务注册
            var app = builder.Build();
            var services = app.Services;

            var eventBusHostedService = services.GetService<IHostedService>();
            eventBusHostedService.GetType().Name.Should().Be("EventBusHostedService");
        }
    }
}
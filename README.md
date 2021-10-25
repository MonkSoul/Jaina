# Jaina

[![license](https://img.shields.io/badge/license-MulanPSL--2.0-orange?cacheSeconds=10800)](https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE) [![nuget](https://img.shields.io/nuget/v/Jaina.svg?cacheSeconds=10800)](https://www.nuget.org/packages/Jaina) [![dotNET China](https://img.shields.io/badge/organization-dotNET%20China-yellow?cacheSeconds=10800)](https://gitee.com/dotnetchina)

一个应用程序框架，您可以将它集成到任何 .NET/C# 应用程序中。

![Jaina Drawio](https://images.gitee.com/uploads/images/2021/1025/131519_ad137395_974299.png "jaina.png")

## 安装

- [Package Manager](https://www.nuget.org/packages/Jaina)

```powershell
Install-Package Jaina
```

- [.NET CLI](https://www.nuget.org/packages/Jaina)

```powershell
dotnet add package Jaina
```

## 快速入门

我们在[主页](https://gitee.com/dotnetchina/Jaina)上有不少例子，这是让您入门的第一个：

1. 定义事件订阅者 `ToDoEventSubscriber`：

```cs
// 实现 IEventSubscriber 接口
public class ToDoEventSubscriber : IEventSubscriber
{
    private readonly ILogger<ToDoEventSubscriber> _logger;
    public ToDoEventSubscriber(ILogger<ToDoEventSubscriber> logger)
    {
        _logger = logger;
    }
    // 标记 [EventSubscribe(事件 Id)] 特性
    [EventSubscribe("ToDo:Create")]
    public async Task CreateToDo(EventHandlerExecutingContext context)
    {
        var todo = context.Source;
        _logger.LogInformation("创建一个 ToDo：{Name}", todo.Payload);
        await Task.CompletedTask;
    }
}
```

2. 创建控制器 `ToDoController`，依赖注入 `IEventPublisher` 服务：

```cs
[Route("api/[controller]/[action]")]
[ApiController]
public class ToDoController : ControllerBase
{
    // 依赖注入事件发布者 IEventPublisher
    private readonly IEventPublisher _eventPublisher;
    public ToDoController(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
    // 发布 ToDo:Create 消息
    [HttpPost]
    public async Task CreateDoTo(string name)
    {
        await _eventPublisher.PublishAsync(new ChannelEventSource("ToDo:Create", name));
    }
}
```

3. 在 `Startup.cs` 注册 `EventBus` 服务：

```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册 EventBus 服务
        services.AddEventBus(buidler =>
        {
            // 注册 ToDo 事件订阅者
            buidler.AddSubscriber<ToDoEventSubscriber>();
        });
        // ....
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // ....
    }
}
```

4. 运行项目：

```log
info: Jaina.Samples.ToDoEventSubscriber[0]
      创建一个 ToDo：Jaina
```

## 高级教程

**1. 自定义事件源 `IEventSource`**

Jaina 使用 `IEventSource` 作为消息载体，任何实现该接口的类都可以充当消息载体。

如需自定义，只需实现 `IEventSource` 接口即可：

```cs
public class ToDoEventSource : IEventSource
{
    // 自定义属性
    public string ToDoName { get; }

    /// <summary>
    /// 事件 Id
    /// </summary>
    public string EventId { get; }

    /// <summary>
    /// 事件承载（携带）数据
    /// </summary>
    public object Payload { get; }

    /// <summary>
    /// 取消任务 Token
    /// </summary>
    /// <remarks>用于取消本次消息处理</remarks>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// 事件创建时间
    /// </summary>
    public DateTime CreatedTime { get; } = DateTime.UtcNow; 
}
```

使用：

```cs
await _eventPublisher.PublishAsync(new ToDoEventSource {
    EventId = "ToDo:Create",
    ToDoName = "我的 ToDo Name"
});
```

**2. 自定义事件源存储器 `IEventSourceStorer`**

Jaina 默认采用 `Channel` 作为事件源 `IEventSource` 存储器，开发者可以使用任何消息队列组件进行替换，如 `Kafka、RabbitMQ、ActiveMQ` 等，也可以使用部分数据库 `Redis、SQL Server、MySql` 实现。

如需自定义，只需实现 `IEventSourceStorer` 接口即可：

```cs
public class RedisEventSourceStorer : IEventSourceStorer
{
    private readonly IRedisClient _redisClient;

    public RedisEventSourceStorer(IRedisClient redisClient)
    {
        _redisClient = redisClient;
    }

    // 往 Redis 中写入一条
    public async ValueTask WriteAsync(IEventSource eventSource, CancellationToken cancellationToken)
    {
        await _redisClient.WriteAsync(...., cancellationToken);
    }
    
    // 从 Redis 中读取一条
    public async ValueTask<IEventSource> ReadAsync(CancellationToken cancellationToken)
    {
       return await _redisClient.ReadAsync(...., cancellationToken);
    }
}
```

最后，在注册 `EventBus` 服务中替换默认 `IEventSourceStorer`：

```cs
services.AddEventBus(buidler =>
{
    // 替换事件源存储器
    buidler.ReplaceStorer<RedisEventSourceStorer>();
});
```

**3. 自定义事件发布者 `IEventPublisher`**

Jaina 默认内置基于 `Channel` 的事件发布者 `ChannelEventPublisher`。

如需自定义，只需实现 `ChannelEventPublisher` 接口即可：

```cs
public class ToDoEventPublisher : IEventPublisher
{
    private readonly IEventSourceStorer _eventSourceStorer;
    
    public ChannelEventPublisher(IEventSourceStorer eventSourceStorer)
    {
        _eventSourceStorer = eventSourceStorer;
    }
    
    public async Task PublishAsync(IEventSource eventSource)
    {
        await _eventSourceStorer.WriteAsync(eventSource, eventSource.CancellationToken);
    }
}
```

最后，在注册 `EventBus` 服务中替换默认 `IEventPublisher`：

```cs
services.AddEventBus(buidler =>
{
    // 替换事件源存储器
    buidler.ReplacePublisher<ToDoEventPublisher>();
});
```

**4. 添加事件执行监视器 `IEventHandlerMonitor`**

Jaina 提供了 `IEventHandlerMonitor` 监视器接口，实现该接口可以监视所有订阅事件，包括 `执行之前、执行之后，执行异常，共享上下文数据`。

如添加 `ToDoEventHandlerMonitor`：

```cs
public class ToDoEventHandlerMonitor : IEventHandlerMonitor
{
    private readonly ILogger<ToDoEventHandlerMonitor> _logger;
    public ToDoEventHandlerMonitor(ILogger<ToDoEventHandlerMonitor> logger)
    {
        _logger = logger;
    }

    public Task OnExecutingAsync(EventHandlerExecutingContext context)
    {
        _logger.LogInformation("执行之前：{EventId}", context.Source.EventId);
        return Task.CompletedTask;
    }

    public Task OnExecutedAsync(EventHandlerExecutedContext context)
    {
        _logger.LogInformation("执行之后：{EventId}", context.Source.EventId);

        if (context.Exception != null)
        {
            _logger.LogError(context.Exception, "执行出错啦：{EventId}", context.Source.EventId);
        }

        return Task.CompletedTask;
    }
}
```

最后，在注册 `EventBus` 服务中注册 `ToDoEventHandlerMonitor`：

```cs
services.AddEventBus(buidler =>
{
    // 主键事件执行监视器
    buidler.AddMonitor<ToDoEventHandlerMonitor>();
});
```

**5. 自定义事件处理程序执行器 `IEventHandlerExecutor`**

Jaina 提供了 `IEventHandlerExecutor` 执行器接口，可以让开发者自定义事件处理函数执行策略，如 `超时控制，失败重试、熔断等等`。

如添加 `RetryEventHandlerExecutor`：

```cs
public class RetryEventHandlerExecutor : IEventHandlerExecutor
{
    public async Task ExecuteAsync(EventHandlerExecutingContext context, Func<EventHandlerExecutingContext, Task> handler)
    {
        // 如果执行失败，每隔 1s 重试，最多三次
        await Retry(async () => {
            await handler(context);
        }, 3, 1000);
    }
}
```

最后，在注册 `EventBus` 服务中注册 `RetryEventHandlerExecutor`：

```cs
services.AddEventBus(buidler =>
{
    // 主键事件执行监视器
    buidler.AddExecutor<RetryEventHandlerExecutor>();
});
```

## 文档

您可以在[主页](https://dotnetchina.gitee.io/Jaina)找到 Jaina 文档。

## 贡献

该存储库的主要目的是继续发展 Jaina 核心，使其更快、更易于使用。Jaina 的开发在 [Gitee](https://gitee.com/dotnetchina/Jaina) 上公开进行，我们感谢社区贡献错误修复和改进。

## 许可证

Jaina 采用 [MulanPSL-2.0](https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE) 开源许可证。

```
Copyright (c) 2020-2021 百小僧, Baiqian Co.,Ltd.
Jaina is licensed under Mulan PSL v2.
You can use this software according to the terms andconditions of the Mulan PSL v2.
You may obtain a copy of Mulan PSL v2 at:
            https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE
THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUTWARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED,INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT,MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
See the Mulan PSL v2 for more details.
```

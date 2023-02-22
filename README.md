# Jaina

[![license](https://img.shields.io/badge/license-MIT-orange?cacheSeconds=10800)](https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE) [![nuget](https://img.shields.io/nuget/v/Jaina.svg?cacheSeconds=10800)](https://www.nuget.org/packages/Jaina) [![dotNET China](https://img.shields.io/badge/organization-dotNET%20China-yellow?cacheSeconds=10800)](https://gitee.com/dotnetchina)

.NET 事件总线，简化项目、类库、线程、服务等之间的通信，代码更少，质量更好。‎

![Jaina.drawio](https://gitee.com/dotnetchina/Jaina/raw/master/drawio/Jaina.drawio.png "Jaina.drawio.png")

[源码解析](./PRINCIPLE.md)

## 特性

- 简化组件之间通信
  - 支持事件监视器
  - 支持动作执行器
  - 支持自定义消息存储组件
  - 支持自定义策略执行
  - 支持单消费、多消费消息
  - 支持消息幂等性处理
- 高内聚，低耦合，使代码更简单
- 非常快速，每秒可处理 `30000 +` 消息
- 很小，仅 `10KB`
- 无第三方依赖
- 可在 `Windows/Linux/MacOS` 守护进程部署
- 支持分布式、集群
- 高质量代码和良好单元测试

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

我们在[主页](./samples)上有不少例子，这是让您入门的第一个：

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

    [EventSubscribe("ToDo:Create")] // 支持多个
    [EventSubscribe(YourEnum.Message)]   // 支持枚举
    public async Task CreateToDo(EventHandlerExecutingContext context)
    {
        var todo = context.Source;
        _logger.LogInformation("创建一个 ToDo：{Name}", todo.Payload);
        await Task.CompletedTask;
    }

    // 支持枚举类型
    [EventSubscribe(YourEnum.Some)]
    public async Task EnumHandler(EventHandlerExecutingContext context)
    {
        var eventEnum = context.Source.EventId.ParseToEnum(); // 将事件 Id 转换成枚举对象
        await Task.CompletedTask;
    }

    // 支持正则表达式匹配
    [EventSubscribe("(^1[3456789][0-9]{9}$)|((^[0-9]{3,4}\\-[0-9]{3,8}$)|(^[0-9]{3,8}$)|(^\\([0-9]{3,4}\\)[0-9]{3,8}$)|(^0{0,1}13[0-9]{9}$))", FuzzyMatch = true)]
    public async Task RegexHandler(EventHandlerExecutingContext context)
    {
        var eventId = context.Source.EventId;
        await Task.CompletedTask;
    }

    // 支持多种异常重试配置
    [EventSubscribe("test:error", NumRetries = 3)]
    [EventSubscribe("test:error", NumRetries = 3, RetryTimeout = 1000)] // 重试间隔时间
    [EventSubscribe("test:error", NumRetries = 3, ExceptionTypes = new[] { typeof(ArgumentException) })]    // 特定类型异常才重试
    public async Task ExceptionHandler(EventHandlerExecutingContext context)
    {
        var eventId = context.Source.EventId;
        await Task.CompletedTask;
    }
}
```

2. 创建控制器 `ToDoController`，依赖注入 `IEventPublisher` 服务：

```cs
public class ToDoController : ControllerBase
{
    // 依赖注入事件发布者 IEventPublisher
    private readonly IEventPublisher _eventPublisher;
    public ToDoController(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    // 发布 ToDo:Create 消息
    public async Task CreateDoTo(string name)
    {
        await _eventPublisher.PublishAsync(new ChannelEventSource("ToDo:Create", name));
        // 简化版本
        await _eventPublisher.PublishAsync("ToDo:Create", name);
    }
}
```

3. 在 `Startup.cs` 注册 `EventBus` 服务：

```cs
// 注册 EventBus 服务
services.AddEventBus(builder =>
{
    // 注册 ToDo 事件订阅者
    builder.AddSubscriber<ToDoEventSubscriber>();

    // 通过类型注册
    builder.AddSubscriber(typeof(ToDoEventSubscriber));

    // 批量注册事件订阅者
    builder.AddSubscribers(ass1, ass2, ....);
});
```

4. 运行项目：

```bash
info: Jaina.Samples.ToDoEventSubscriber[0]
      创建一个 ToDo：Jaina
```

[更多文档](https://furion.baiqian.ltd/docs/event-bus)

## 文档

您可以在[主页](https://furion.baiqian.ltd/docs/event-bus)找到 Jaina 文档。

## 贡献

该存储库的主要目的是继续发展 Jaina 核心，使其更快、更易于使用。Jaina 的开发在 [Gitee](https://gitee.com/dotnetchina/Jaina) 上公开进行，我们感谢社区贡献错误修复和改进。

## 许可证

Jaina 采用 [MIT](./LICENSE) 开源许可证。

```
MIT License

Copyright (c) 2020-present 百小僧, Baiqian Co.,Ltd.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
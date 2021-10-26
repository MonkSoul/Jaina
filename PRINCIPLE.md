![Jaina.drawio](https://gitee.com/dotnetchina/Jaina/raw/master/drawio/Jaina.drawio.png "Jaina.drawio.png")

## 源码解析

`EventBusHostedService` 类是事件总线的核心服务，派生自 `BackgroundService`，随服务自启动并长时间执行任务。其主要作用有：
1. 扫描所有 `IEventSubscriber` 实现类，遍历每个实现类类型和获取其所有公开贴有 `[EventSubscribe]` 特性的实例方法。
2. 将获得的实例方法转换成 `Func<EventHandlerExecutingContext, Task>` 委托并通过 `EventHandlerWrapper` 类进行包装存储到 `_eventHandlers` 私有字段中。
3. 重写基类 `BackgroundService` 的 `ExecuteAsync` 方法，通过 `while(服务未停止)` 方式监听 `IEventSourceStorer` 是否有消息写入。
如果有，查询 `_eventHandlers` 集合中所有符合的 `EventHandlerWrapper` 对象：

```cs
// 监听是否有消息写入
while (!stoppingToken.IsCancellationRequested)
{
    // 读取一条消息
    var eventSource = await _eventSourceStorer.ReadAsync(stoppingToken);

    // 获取所有符合消息Id规则的 `EventHandlerWrapper`
    var eventHandlersThatShouldRun = _eventHandlers.Where(t => t.ShouldRun(eventSource.EventId));
}
```

4. 查询到符合的 `EventHandlerWrapper` 包装类后创建基于当前线程 `TaskFactory` 任务工厂，接着循环通过 `taskFactory.StartNew(delegete)` 创建新的 `Task` 对象执行 `EventHandlerWrapper` 中的 `Handler` 属性（事件处理程序）。

```cs
// 逐条创建新线程调用
foreach (var eventHandlerThatShouldRun in eventHandlersThatShouldRun)
{
    // 创建新的 Task
    await taskFactory.StartNew(async () =>
    {
        // ....

        // 调用 Handler
        await eventHandlerThatShouldRun.Handler!(eventHandlerExecutingContext);

        // ....
    }, stoppingCts.Token);
}
```

----

## 目录结构

```
|-- Jaina
    |-- ChannelEventPublisher.cs                          事件发布者默认实现（基于 Channel）
    |-- IEventPublisher.cs                                事件发布者规范接口
    |-- IEventSubscriber.cs                               事件订阅者规范接口
    |-- Attributes                                        特性目录
    |   |-- EventSubscribeAttribute.cs                    订阅特性：将事件处理程序和事件 Id进行绑定
    |-- Builders                                          构建器目录
    |   |-- EventBusOptionsBuilder.cs                     事件总线服务选项构建器，用于构建参与者服务
    |-- Contexts                                          上下文目录
    |   |-- EventHandlerContext.cs                        事件处理程序上下文：包装事件源和添加额外属性
    |   |-- EventHandlerExecutedContext.cs                事件处理程序执行上下文（执行后）
    |   |-- EventHandlerExecutingContext.cs               事件处理程序执行上下文（执行前）
    |-- Executors                                         执行器目录
    |   |-- IEventHandlerExecutor.cs                      事件处理程序执行器：可以对执行方法进行包装
    |-- Extensions                                        拓展目录
    |   |-- EventBusServiceCollectionExtensions.cs        事件总线服务拓展类
    |-- HostedServices                                    主机服务目录
    |   |-- EventBusHostedService.cs                      事件总线服务中心：对事件源进行包装、分发、触发
    |-- Monitors                                          监视器目录
    |   |-- IEventHandlerMonitor.cs                       事件处理程序监视器规范接口
    |-- Sources                                           事件源目录
    |   |-- ChannelEventSource.cs                         事件源默认实现（基于 Channel）
    |   |-- IEventSource.cs                               事件源规范接口
    |-- Storers                                           存储器目录
    |   |-- ChannelEventSourceStorer.cs                   事件源存储器默认实现（基于 Channel）
    |   |-- IEventSourceStorer.cs                         事件源存储器规范接口
    |-- Wrappers                                          包装器目录
        |-- EventHandlerWrapper.cs                        事件处理程序包装类
```
﻿namespace Jaina.Samples;

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
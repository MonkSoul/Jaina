using Microsoft.AspNetCore.Mvc;

namespace Jaina.Samples.Controllers;

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

    // 延迟发布 ToDo:Create 消息
    [HttpPost]
    public async Task DelayCreateDoTo(string name)
    {
        await _eventPublisher.PublishDelayAsync(new ChannelEventSource("ToDo:Create", name), 3000);
    }
}
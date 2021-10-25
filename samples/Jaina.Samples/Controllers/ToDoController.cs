using Jaina.EventBus;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Jaina.Samples.Controllers
{
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
}
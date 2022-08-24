// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Jaina is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using System.Reflection;

namespace Jaina.EventBus;

/// <summary>
/// 事件总线工厂默认实现
/// </summary>
internal class EventBusFactory : IEventBusFactory
{
    /// <summary>
    /// 事件源存储器
    /// </summary>
    private readonly IEventSourceStorer _eventSourceStorer;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSourceStorer">事件源存储器</param>
    public EventBusFactory(IEventSourceStorer eventSourceStorer)
    {
        _eventSourceStorer = eventSourceStorer;
    }

    /// <summary>
    /// 添加事件订阅者
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="handler"></param>
    /// <param name="attribute"></param>
    /// <param name="handlerMethod"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task AddSubscriber(string eventId, Func<EventHandlerExecutingContext, Task> handler, EventSubscribeAttribute attribute = default, MethodInfo handlerMethod = default, CancellationToken cancellationToken = default)
    {
        // 空检查
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        await _eventSourceStorer.WriteAsync(new EventSubscribeOperateSource
        {
            SubscribeEventId = eventId,
            Attribute = attribute,
            Handler = handler,
            HandlerMethod = handlerMethod,
            Operate = EventSubscribeOperates.Append
        }, cancellationToken);
    }

    /// <summary>
    /// 删除事件订阅者
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task RemoveSubscriber(string eventId, CancellationToken cancellationToken = default)
    {
        // 空检查
        if (eventId == null) throw new ArgumentNullException(nameof(eventId));

        await _eventSourceStorer.WriteAsync(new EventSubscribeOperateSource
        {
            SubscribeEventId = eventId,
            Operate = EventSubscribeOperates.Remove
        }, default);
    }
}
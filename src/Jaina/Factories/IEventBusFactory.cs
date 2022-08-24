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
/// 事件总线工厂接口
/// </summary>
public interface IEventBusFactory
{
    /// <summary>
    /// 添加事件订阅者
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="handler"></param>
    /// <param name="attribute"></param>
    /// <param name="handlerMethod"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddSubscriber(string eventId, Func<EventHandlerExecutingContext, Task> handler, EventSubscribeAttribute attribute = default, MethodInfo handlerMethod = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除事件订阅者
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RemoveSubscriber(string eventId, CancellationToken cancellationToken = default);
}
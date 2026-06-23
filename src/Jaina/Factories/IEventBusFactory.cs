// 版权归百小僧及百签科技（广东）有限公司所有。
//
// 此源代码遵循位于源代码树根目录中的 LICENSE 文件的许可证。

using System.Reflection;

namespace Jaina;

/// <summary>
/// 事件总线工厂接口
/// </summary>
public interface IEventBusFactory
{
    /// <summary>
    /// 添加事件订阅者
    /// </summary>
    /// <param name="eventId">事件 Id</param>
    /// <param name="handler">事件订阅委托</param>
    /// <param name="attribute"><see cref="EventSubscribeAttribute"/> 特性对象</param>
    /// <param name="handlerMethod"><see cref="MethodInfo"/> 对象</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns></returns>
    Task Subscribe(string eventId, Func<EventHandlerExecutingContext, Task> handler, EventSubscribeAttribute attribute = default, MethodInfo handlerMethod = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加事件订阅者
    /// </summary>
    /// <param name="eventId">事件 Id</param>
    /// <param name="handler">事件订阅委托</param>
    /// <param name="attribute"><see cref="EventSubscribeAttribute"/> 特性对象</param>
    /// <param name="handlerMethod"><see cref="MethodInfo"/> 对象</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns></returns>
    Task Subscribe(string eventId, Func<EventHandlerExecutingContext, CancellationToken, Task> handler, EventSubscribeAttribute attribute = default, MethodInfo handlerMethod = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加泛型事件订阅者
    /// </summary>
    /// <typeparam name="T">事件承载（携带）数据类型</typeparam>
    /// <param name="eventId">事件 Id</param>
    /// <param name="handler">泛型事件订阅委托</param>
    /// <param name="attribute"><see cref="EventSubscribeAttribute"/> 特性对象</param>
    /// <param name="handlerMethod"><see cref="MethodInfo"/> 对象</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns></returns>
    Task Subscribe<T>(string eventId, Func<EventHandlerExecutingContext<T>, Task> handler, EventSubscribeAttribute attribute = default, MethodInfo handlerMethod = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加泛型事件订阅者
    /// </summary>
    /// <typeparam name="T">事件承载（携带）数据类型</typeparam>
    /// <param name="eventId">事件 Id</param>
    /// <param name="handler">泛型事件订阅委托</param>
    /// <param name="attribute"><see cref="EventSubscribeAttribute"/> 特性对象</param>
    /// <param name="handlerMethod"><see cref="MethodInfo"/> 对象</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns></returns>
    Task Subscribe<T>(string eventId, Func<EventHandlerExecutingContext<T>, CancellationToken, Task> handler, EventSubscribeAttribute attribute = default, MethodInfo handlerMethod = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除事件订阅者
    /// </summary>
    /// <param name="eventId">事件 Id</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns></returns>
    Task Unsubscribe(string eventId, CancellationToken cancellationToken = default);
}
// 版权归百小僧及百签科技（广东）有限公司所有。
//
// 此源代码遵循位于源代码树根目录中的 LICENSE 文件的许可证。

using System.Reflection;
using System.Text.RegularExpressions;

namespace Jaina;

/// <summary>
/// 事件处理程序包装类
/// </summary>
/// <remarks>主要用于主机服务启动时将所有处理程序和事件 Id 进行包装绑定</remarks>
internal sealed class EventHandlerWrapper
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventId">事件Id</param>
    internal EventHandlerWrapper(string eventId)
    {
        EventId = eventId;
    }

    /// <summary>
    /// 事件 Id
    /// </summary>
    internal string EventId { get; set; }

    /// <summary>
    /// 事件处理程序
    /// </summary>
    internal Func<EventHandlerExecutingContext, CancellationToken, Task> Handler { get; set; }

    /// <summary>
    /// 触发的方法
    /// </summary>
    internal MethodInfo HandlerMethod { get; set; }

    /// <summary>
    /// 订阅特性
    /// </summary>
    internal EventSubscribeAttribute Attribute { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    internal Regex Pattern { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    /// <remarks>数值越大的先执行</remarks>
    internal int Order { get; set; } = 0;
}
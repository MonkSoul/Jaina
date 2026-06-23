// 版权归百小僧及百签科技（广东）有限公司所有。
//
// 此源代码遵循位于源代码树根目录中的 LICENSE 文件的许可证。

using System.Reflection;

namespace Jaina;

/// <summary>
/// 事件处理程序执行前上下文
/// </summary>
public class EventHandlerExecutingContext : EventHandlerContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="properties">共享上下文数据</param>
    /// <param name="handlerMethod">触发的方法</param>
    /// <param name="attribute">订阅特性</param>
    /// <param name="runId">事件运行的唯一标识</param>
    internal EventHandlerExecutingContext(IEventSource eventSource
        , IDictionary<object, object> properties
        , MethodInfo handlerMethod
        , EventSubscribeAttribute attribute
        , string runId)
        : base(eventSource, properties, handlerMethod, attribute, runId)
    {
    }

    /// <summary>
    /// 执行前时间
    /// </summary>
    public DateTime ExecutingTime { get; internal set; }

    /// <summary>
    /// 执行结果
    /// </summary>
    internal object Result { get; set; }

    /// <summary>
    /// 设置执行结果
    /// </summary>
    /// <param name="result"></param>
    public void SetResult(object result)
    {
        Result = result;
    }
}

/// <summary>
/// 泛型事件处理程序执行前上下文
/// </summary>
/// <typeparam name="T">事件承载（携带）数据类型</typeparam>
public sealed class EventHandlerExecutingContext<T> : EventHandlerExecutingContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="properties">共享上下文数据</param>
    /// <param name="handlerMethod">触发的方法</param>
    /// <param name="attribute">订阅特性</param>
    /// <param name="runId">事件运行的唯一标识</param>
    internal EventHandlerExecutingContext(IEventSource eventSource
        , IDictionary<object, object> properties
        , MethodInfo handlerMethod
        , EventSubscribeAttribute attribute
        , string runId)
        : base(eventSource, properties, handlerMethod, attribute, runId)
    {
    }

    /// <summary>
    /// 强类型事件承载（携带）数据
    /// </summary>
    public T Payload => GetPayload<T>();

    /// <summary>
    /// 将泛型事件处理委托包装为双参数委托
    /// </summary>
    /// <param name="typedHandler">事件订阅委托</param>
    /// <returns></returns>
    internal static Func<EventHandlerExecutingContext, CancellationToken, Task> Wrap(Func<EventHandlerExecutingContext<T>, CancellationToken, Task> typedHandler)
    {
        return async (context, cancellationToken) =>
        {
            var typedContext = new EventHandlerExecutingContext<T>(
                context.Source,
                context.Properties,
                context.HandlerMethod,
                context.Attribute,
                context.RunId)
            {
                ExecutingTime = context.ExecutingTime,
                Result = context.Result
            };

            await typedHandler(typedContext, cancellationToken);

            context.Result = typedContext.Result;
        };
    }

    /// <summary>
    /// 将泛型事件处理委托包装为双参数委托
    /// </summary>
    /// <param name="typedHandler">事件订阅委托</param>
    /// <returns></returns>
    internal static Func<EventHandlerExecutingContext, CancellationToken, Task> WrapSingle(Func<EventHandlerExecutingContext<T>, Task> typedHandler)
    {
        return Wrap((ctx, _) => typedHandler(ctx));
    }
}
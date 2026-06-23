// 版权归百小僧及百签科技（广东）有限公司所有。
//
// 此源代码遵循位于源代码树根目录中的 LICENSE 文件的许可证。

using System.Reflection;
using System.Text.Json;

namespace Jaina;

/// <summary>
/// 事件处理程序上下文
/// </summary>
public abstract class EventHandlerContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="properties">共享上下文数据</param>
    /// <param name="handlerMethod">触发的方法</param>
    /// <param name="attribute">订阅特性</param>
    /// <param name="runId">事件运行的唯一标识</param>
    internal EventHandlerContext(IEventSource eventSource
        , IDictionary<object, object> properties
        , MethodInfo handlerMethod
        , EventSubscribeAttribute attribute
        , string runId)
    {
        Source = eventSource;
        Properties = properties;
        HandlerMethod = handlerMethod;
        Attribute = attribute;
        RunId = runId;
    }

    /// <summary>
    /// 事件源（事件承载对象）
    /// </summary>
    public IEventSource Source { get; }

    /// <summary>
    /// 共享上下文数据
    /// </summary>
    public IDictionary<object, object> Properties { get; set; }

    /// <summary>
    /// 触发的方法
    /// </summary>
    /// <remarks>如果是动态订阅，可能为 null</remarks>
    public MethodInfo HandlerMethod { get; }

    /// <summary>
    /// 订阅特性
    /// </summary>
    /// <remarks>如果是动态订阅，可能为 null</remarks>
    public EventSubscribeAttribute Attribute { get; }

    /// <summary>
    /// 事件运行的唯一标识
    /// </summary>
    public string RunId { get; }

    /// <summary>
    /// 获取负载数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetPayload<T>()
    {
        var rawPayload = Source.Payload;

        if (rawPayload is null)
        {
            return default;
        }
        else if (rawPayload is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), new JsonSerializerOptions(JsonSerializerOptions.Default)
            {
                PropertyNameCaseInsensitive = true
            });
        }
        else
        {
            return (T)rawPayload;
        }
    }
}
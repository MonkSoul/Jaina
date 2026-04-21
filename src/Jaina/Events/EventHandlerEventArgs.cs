// 版权归百小僧及百签科技（广东）有限公司所有。
//
// 此源代码遵循位于源代码树根目录中的 LICENSE 文件的许可证。

namespace Jaina;

/// <summary>
/// 事件处理程序事件参数
/// </summary>
public sealed class EventHandlerEventArgs : EventArgs
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="success">任务处理委托调用结果</param>
    /// <param name="runId">事件运行的唯一标识</param>
    public EventHandlerEventArgs(IEventSource eventSource, bool success, string runId)
    {
        Source = eventSource;
        Status = success ? "SUCCESS" : "FAIL";
        RunId = runId;
    }

    /// <summary>
    /// 事件源（事件承载对象）
    /// </summary>
    public IEventSource Source { get; }

    /// <summary>
    /// 执行状态
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception Exception { get; internal set; }

    /// <summary>
    /// 执行结果
    /// </summary>
    public object Result { get; internal set; }

    /// <summary>
    /// 事件运行的唯一标识
    /// </summary>
    public string RunId { get; }
}
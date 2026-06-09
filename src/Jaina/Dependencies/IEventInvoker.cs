// 版权归百小僧及百签科技（广东）有限公司所有。
//
// 此源代码遵循位于源代码树根目录中的 LICENSE 文件的许可证。

namespace Jaina;

/// <summary>
/// 事件触发器接口
/// </summary>
public interface IEventInvoker
{
    /// <summary>
    /// 触发事件处理程序事件
    /// </summary>
    /// <param name="args">事件参数</param>
    void InvokeEvents(EventHandlerEventArgs args);
}

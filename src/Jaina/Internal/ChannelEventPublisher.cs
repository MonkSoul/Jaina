// 版权归百小僧及百签科技（广东）有限公司所有。
//
// 此源代码遵循位于源代码树根目录中的 LICENSE 文件的许可证。

namespace Jaina;

/// <summary>
/// 基于内存通道事件发布者（默认实现）
/// </summary>
internal sealed partial class ChannelEventPublisher : IEventPublisher, IEventInvoker
{
    /// <summary>
    /// 事件处理程序事件
    /// </summary>
    public event EventHandler<EventHandlerEventArgs> OnExecuted;

    /// <summary>
    /// 事件源存储器
    /// </summary>
    private readonly IEventSourceStorer _eventSourceStorer;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSourceStorer">事件源存储器</param>
    public ChannelEventPublisher(IEventSourceStorer eventSourceStorer)
    {
        _eventSourceStorer = eventSourceStorer;
    }

    /// <inheritdoc/>
    public async Task PublishAsync(IEventSource eventSource, CancellationToken cancellationToken = default)
    {
        await _eventSourceStorer.WriteAsync(eventSource, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task PublishAsync(string eventId, object payload = default, CancellationToken cancellationToken = default)
    {
        await PublishAsync(new ChannelEventSource(eventId, payload), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task PublishAsync(Enum eventId, object payload = default, CancellationToken cancellationToken = default)
    {
        await PublishAsync(new ChannelEventSource(eventId, payload), cancellationToken);
    }

    /// <inheritdoc/>
    public Task PublishDelayAsync(IEventSource eventSource, long delay, CancellationToken cancellationToken = default)
    {
        _ = SafeDelayPublishAsync(eventSource, TimeSpan.FromMilliseconds(delay), cancellationToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task PublishDelayAsync(string eventId, long delay, object payload = default, CancellationToken cancellationToken = default)
    {
        await PublishDelayAsync(new ChannelEventSource(eventId, payload), delay, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task PublishDelayAsync(Enum eventId, long delay, object payload = default, CancellationToken cancellationToken = default)
    {
        await PublishDelayAsync(new ChannelEventSource(eventId, payload), delay, cancellationToken);
    }

    /// <inheritdoc/>
    public Task PublishDelayAsync(IEventSource eventSource, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        _ = SafeDelayPublishAsync(eventSource, delay, cancellationToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task PublishDelayAsync(string eventId, TimeSpan delay, object payload = null, CancellationToken cancellationToken = default)
    {
        await PublishDelayAsync(new ChannelEventSource(eventId, payload), delay, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task PublishDelayAsync(Enum eventId, TimeSpan delay, object payload = null, CancellationToken cancellationToken = default)
    {
        await PublishDelayAsync(new ChannelEventSource(eventId, payload), delay, cancellationToken);
    }

    /// <summary>
    /// 安全延迟发布内部方法
    /// </summary>
    /// <param name="eventSource">事件源</param>
    /// <param name="delay">延迟数</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    private async Task SafeDelayPublishAsync(IEventSource eventSource, TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            // 延迟 delay 毫秒
            await Task.Delay(delay, cancellationToken);
            await _eventSourceStorer.WriteAsync(eventSource, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EventBus delay publish failed: {ex.Message}");
        }
    }

    void IEventInvoker.InvokeEvents(EventHandlerEventArgs args)
    {
        try
        {
            OnExecuted?.Invoke(this, args);
        }
        catch { }
    }
}
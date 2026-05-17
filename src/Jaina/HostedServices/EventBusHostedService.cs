// 版权归百小僧及百签科技（广东）有限公司所有。
//
// 此源代码遵循位于源代码树根目录中的 LICENSE 文件的许可证。

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Logging;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Jaina;

/// <summary>
/// 事件总线后台主机服务
/// </summary>
internal sealed class EventBusHostedService : BackgroundService
{
    /// <summary>
    /// 避免由 CLR 的终结器捕获该异常从而终止应用程序，让所有未觉察异常被觉察
    /// </summary>
    internal event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;

    /// <summary>
    /// 日志对象
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// 服务提供器
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 事件源存储器
    /// </summary>
    private readonly IEventSourceStorer _eventSourceStorer;

    /// <summary>
    /// 事件发布服务
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 精准匹配事件处理程序集合
    /// </summary>
    private readonly ConcurrentDictionary<string, List<EventHandlerWrapper>> _exactHandlers = new();

    /// <summary>
    /// 正则匹配的事件处理程序集合
    /// </summary>
    private readonly List<EventHandlerWrapper> _regexHandlers = [];

    /// <summary>
    /// 追踪当前正在运行的事件处理任务
    /// </summary>
    private readonly ConcurrentBag<Task> _runningTasks = [];

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志对象</param>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="eventSourceStorer">事件源存储器</param>
    /// <param name="eventPublisher">事件发布服务</param>
    /// <param name="eventSubscribers">事件订阅者集合</param>
    /// <param name="useUtcTimestamp">是否使用 Utc 时间</param>
    /// <param name="fuzzyMatch">是否启用模糊匹配事件消息</param>
    /// <param name="logEnabled">是否启用日志记录</param>
    public EventBusHostedService(ILogger<EventBusService> logger
        , IServiceProvider serviceProvider
        , IEventSourceStorer eventSourceStorer
        , IEventPublisher eventPublisher
        , IEnumerable<IEventSubscriber> eventSubscribers
        , bool useUtcTimestamp
        , bool fuzzyMatch
        , bool logEnabled)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventPublisher = eventPublisher;
        _eventSourceStorer = eventSourceStorer;

        Monitor = serviceProvider.GetService<IEventHandlerMonitor>();
        Executor = serviceProvider.GetService<IEventHandlerExecutor>();
        UseUtcTimestamp = useUtcTimestamp;
        FuzzyMatch = fuzzyMatch;
        LogEnabled = logEnabled;

        var bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        // 逐条获取事件处理程序并进行包装
        foreach (var eventSubscriber in eventSubscribers)
        {
            // 获取事件订阅者类型
            var eventSubscriberType = eventSubscriber.GetType();

            // 查找所有公开且贴有 [EventSubscribe] 的实例方法
            var eventHandlerMethods = eventSubscriberType.GetMethods(bindingAttr)
                .Where(u => u.IsDefined(typeof(EventSubscribeAttribute), false));

            // 遍历所有事件订阅者处理方法
            foreach (var eventHandlerMethod in eventHandlerMethods)
            {
                // 将方法转换成 Func<EventHandlerExecutingContext, Task> 委托
                var handler = (Func<EventHandlerExecutingContext, Task>)eventHandlerMethod.CreateDelegate(typeof(Func<EventHandlerExecutingContext, Task>), eventSubscriber);

                // 处理同一个事件处理程序支持多个事件 Id 情况
                var eventSubscribeAttributes = eventHandlerMethod.GetCustomAttributes<EventSubscribeAttribute>(false);

                // 逐条包装并添加到集合中
                foreach (var eventSubscribeAttribute in eventSubscribeAttributes)
                {
                    var wrapper = new EventHandlerWrapper(eventSubscribeAttribute.EventId)
                    {
                        Handler = handler,
                        HandlerMethod = eventHandlerMethod,
                        Attribute = eventSubscribeAttribute,
                        Pattern = CheckIsSetFuzzyMatch(eventSubscribeAttribute.FuzzyMatch) ? new Regex(eventSubscribeAttribute.EventId, RegexOptions.Singleline) : null,
                        Order = eventSubscribeAttribute.Order
                    };

                    AddEventHandlerWrapper(wrapper);
                }
            }
        }
    }

    /// <summary>
    /// 添加事件处理程序包装器到相应集合
    /// </summary>
    /// <param name="wrapper"></param>
    private void AddEventHandlerWrapper(EventHandlerWrapper wrapper)
    {
        if (wrapper.Pattern == null)
        {
            // 精确匹配
            _exactHandlers.AddOrUpdate(wrapper.EventId, _ => [wrapper], (_, list) =>
            {
                lock (list)
                {
                    list.Add(wrapper);
                }
                return list;
            });
        }
        else
        {
            // 正则匹配
            lock (_regexHandlers)
            {
                _regexHandlers.Add(wrapper);
            }
        }
    }

    /// <summary>
    /// 事件处理程序监视器
    /// </summary>
    private IEventHandlerMonitor Monitor { get; }

    /// <summary>
    /// 事件处理程序执行器
    /// </summary>
    private IEventHandlerExecutor Executor { get; }

    /// <summary>
    /// 是否使用 UTC 时间
    /// </summary>
    private bool UseUtcTimestamp { get; }

    /// <summary>
    /// 是否启用模糊匹配事件消息
    /// </summary>
    private bool FuzzyMatch { get; }

    /// <summary>
    /// 是否启用日志记录
    /// </summary>
    private bool LogEnabled { get; }

    /// <summary>
    /// 执行后台任务
    /// </summary>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log(LogLevel.Information, "EventBus hosted service is running.");

        // 注册后台主机服务停止监听
        stoppingToken.Register(() => Log(LogLevel.Debug, $"EventBus hosted service is stopping."));

        // 监听服务是否取消
        while (!stoppingToken.IsCancellationRequested)
        {
            // 执行具体任务
            await BackgroundProcessing(stoppingToken);
        }

        Log(LogLevel.Warning, $"EventBus hosted service is stopped.");
    }

    /// <summary>
    /// 后台调用处理程序
    /// </summary>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        // 从事件存储器中读取一条
        var eventSource = await _eventSourceStorer.ReadAsync(stoppingToken);

        // 空检查
        if (eventSource is null)
        {
            return;
        }

        // 处理动态新增/删除事件订阅器
        if (eventSource is EventSubscribeOperateSource subscribeOperateSource)
        {
            ManageEventSubscribers(subscribeOperateSource);

            return;
        }

        // 空检查
        if (string.IsNullOrWhiteSpace(eventSource?.EventId))
        {
            Log(LogLevel.Warning, "Invalid EventId, EventId cannot be <null> or an empty string.");
            return;
        }

        // 查找事件 Id 匹配的事件处理程序
        var eventHandlersThatShouldRun = GetMatchingHandlers(eventSource.EventId);

        // 空订阅
        if (eventHandlersThatShouldRun.Count == 0)
        {
            Log(LogLevel.Warning, "Subscriber with event ID <{EventId}> was not found.", [eventSource.EventId]);

            return;
        }

        // 检查是否配置只消费一次
        if (eventSource.ConsumeOnce)
        {
            var randomId = RandomNumberGenerator.GetInt32(0, eventHandlersThatShouldRun.Count);
            eventHandlersThatShouldRun = [eventHandlersThatShouldRun[randomId]];
        }

        // 遍历所有匹配的事件处理程序并执行
        foreach (var eventHandler in eventHandlersThatShouldRun)
        {
            // 创建共享上下文数据对象
            var properties = new Dictionary<object, object>();

            // 执行事件处理程序并跟踪任务
            var task = ExecuteEventHandlerAsync(eventSource, eventHandler, properties, stoppingToken);
            _runningTasks.Add(task);

            // 任务完成后自动从集合中移除
            _ = task.ContinueWith(t => _runningTasks.TryTake(out _), TaskContinuationOptions.ExecuteSynchronously);
        }
    }

    /// <summary>
    /// 获取匹配的事件处理程序列表
    /// </summary>
    /// <param name="eventId"></param>
    private List<EventHandlerWrapper> GetMatchingHandlers(string eventId)
    {
        var handlers = new List<EventHandlerWrapper>();

        // 精确匹配
        if (_exactHandlers.TryGetValue(eventId, out var exactList))
        {
            handlers.AddRange(exactList);
        }

        // 正则匹配
        foreach (var regexWrapper in _regexHandlers)
        {
            if (regexWrapper.Pattern!.IsMatch(eventId))
                handlers.Add(regexWrapper);
        }

        // 按 Order 降序排序
        return handlers.OrderByDescending(h => h.Order).ToList();
    }

    /// <summary>
    /// 执行单个事件处理程序逻辑
    /// </summary>
    private async Task ExecuteEventHandlerAsync(IEventSource eventSource, EventHandlerWrapper eventHandler, Dictionary<object, object> properties, CancellationToken stoppingToken)
    {
        // 创建本次事件运行唯一标识
        var runId = $"{Guid.NewGuid()}";

        // 获取特性信息，可能为 null
        var eventSubscribeAttribute = eventHandler.Attribute;

        // 合并取消令牌并通过上下文传递
        var cancellationToken = stoppingToken;

        // 创建执行前上下文
        var eventHandlerExecutingContext = new EventHandlerExecutingContext(eventSource, properties, eventHandler.HandlerMethod, eventSubscribeAttribute, runId, cancellationToken)
        {
            ExecutingTime = UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now
        };

        // 执行异常对象
        InvalidOperationException executionException = default;

        try
        {
            // 处理任务取消
            cancellationToken.ThrowIfCancellationRequested();

            // 调用执行前监视器
            if (Monitor != null)
            {
                await Monitor.OnExecutingAsync(eventHandlerExecutingContext);
            }

            // 判断是否自定义了执行器
            if (Executor == null)
            {
                // 判断是否自定义了重试失败回调服务
                var fallbackPolicyService = eventSubscribeAttribute?.FallbackPolicy == null
                    ? null
                    : _serviceProvider.GetService(eventSubscribeAttribute.FallbackPolicy) as IEventFallbackPolicy;

                // 调用事件处理程序并配置出错执行重试
                await Retry.InvokeAsync(async () =>
                {
                    await eventHandler.Handler!(eventHandlerExecutingContext);
                }
                , eventSubscribeAttribute?.NumRetries ?? 0
                , eventSubscribeAttribute?.RetryTimeout ?? 1000
                , exceptionTypes: eventSubscribeAttribute?.ExceptionTypes
                , fallbackPolicy: fallbackPolicyService == null ? null : async (ex) => await fallbackPolicyService.CallbackAsync(eventHandlerExecutingContext, ex)
                , retryAction: (total, times) =>
                {
                    // 输出重试日志
                    Log(LogLevel.Warning, "Retrying {times}/{total} times for {EventId}", [times, total, eventSource.EventId]);
                });
            }
            else
            {
                await Executor.ExecuteAsync(eventHandlerExecutingContext, eventHandler.Handler!);
            }

            // 触发事件处理程序事件
            _eventPublisher.InvokeEvents(new(eventSource, true, runId)
            {
                Result = eventHandlerExecutingContext.Result
            });
        }
        catch (Exception ex)
        {
            // 输出异常日志
            Log(LogLevel.Error, "Error occurred executing in {EventId}.", [eventSource.EventId], ex);

            // 标记异常
            executionException = new InvalidOperationException(string.Format("Error occurred executing in {0}.", eventSource.EventId), ex);

            // 捕获 Task 任务异常信息并统计所有异常
            if (UnobservedTaskException != null)
            {
                var args = new UnobservedTaskExceptionEventArgs(
                    ex as AggregateException ?? new AggregateException(ex));

                UnobservedTaskException.Invoke(this, args);
            }

            // 触发事件处理程序事件
            _eventPublisher.InvokeEvents(new(eventSource, false, runId)
            {
                Exception = ex,
                Result = eventHandlerExecutingContext.Result
            });
        }
        finally
        {
            // 调用执行后监视器
            if (Monitor != null)
            {
                // 创建执行后上下文
                var eventHandlerExecutedContext = new EventHandlerExecutedContext(eventSource, properties, eventHandler.HandlerMethod, eventSubscribeAttribute, runId, cancellationToken)
                {
                    ExecutedTime = UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now,
                    Exception = executionException
                };

                await Monitor.OnExecutedAsync(eventHandlerExecutedContext);
            }
        }
    }

    /// <summary>
    /// 管理事件订阅器动态
    /// </summary>
    /// <param name="subscribeOperateSource"></param>
    private void ManageEventSubscribers(EventSubscribeOperateSource subscribeOperateSource)
    {
        // 获取实际订阅事件 Id
        var eventId = subscribeOperateSource.SubscribeEventId;

        // 确保事件订阅 Id 和传入的特性 EventId 一致
        if (subscribeOperateSource.Attribute != null && subscribeOperateSource.Attribute.EventId != eventId) throw new InvalidOperationException("Ensure that the <eventId> is consistent with the <EventId> attribute of the EventSubscribeAttribute object.");

        // 处理动态新增
        if (subscribeOperateSource.Operate == EventSubscribeOperates.Append)
        {
            var wrapper = new EventHandlerWrapper(eventId)
            {
                Attribute = subscribeOperateSource.Attribute,
                HandlerMethod = subscribeOperateSource.HandlerMethod,
                Handler = subscribeOperateSource.Handler,
                Pattern = CheckIsSetFuzzyMatch(subscribeOperateSource.Attribute?.FuzzyMatch) ? new Regex(eventId, RegexOptions.Singleline) : null,
                Order = subscribeOperateSource.Attribute?.Order ?? 0
            };

            // 追加到集合中
            AddEventHandlerWrapper(wrapper);

            // 输出日志
            Log(LogLevel.Information, "Subscriber with event ID <{EventId}> was appended successfully.", [eventId]);
        }
        // 处理动态删除
        else if (subscribeOperateSource.Operate == EventSubscribeOperates.Remove)
        {
            // 精确匹配删除
            if (_exactHandlers.TryGetValue(eventId, out var list))
            {
                lock (list)
                {
                    var toRemove = list.Where(w => w.EventId == eventId).ToList();
                    foreach (var wrapper in toRemove)
                    {
                        list.Remove(wrapper);
                        Log(LogLevel.Warning, "Subscriber<{Name}> with event ID <{EventId}> was removed.", [wrapper.HandlerMethod?.Name, eventId]);
                    }
                    if (list.Count == 0)
                        _exactHandlers.TryRemove(eventId, out _);
                }
            }

            // 正则匹配删除
            lock (_regexHandlers)
            {
                var toRemove = _regexHandlers.Where(w => w.EventId == eventId).ToList();
                foreach (var wrapper in toRemove)
                {
                    _regexHandlers.Remove(wrapper);
                    Log(LogLevel.Warning, "Subscriber<{Name}> with event ID <{EventId}> was removed.", [wrapper.HandlerMethod?.Name, eventId]);
                }
            }
        }
    }

    /// <summary>
    /// 检查是否开启模糊匹配事件 Id 功能
    /// </summary>
    /// <param name="fuzzyMatch"></param>
    /// <returns></returns>
    private bool CheckIsSetFuzzyMatch(object fuzzyMatch)
    {
        return fuzzyMatch == null
            ? FuzzyMatch
            : Convert.ToBoolean(fuzzyMatch);
    }

    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="logLevel">日志级别</param>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    /// <param name="ex">异常</param>
    private void Log(LogLevel logLevel, string message, object[] args = null, Exception ex = null)
    {
        // 如果未启用日志记录则直接返回
        if (!LogEnabled) return;

        if (logLevel == LogLevel.Error)
        {
            _logger.LogError(ex, message, args);
        }
        else
        {
            _logger.Log(logLevel, message, args);
        }
    }

    /// <summary>
    /// 监听事件总线服务服务停止
    /// </summary>
    /// <param name="cancellationToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/></returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Log(LogLevel.Information, "EventBus hosted service is stopping...");

        // 等待正在运行的事件处理程序完成
        if (!_runningTasks.IsEmpty)
        {
            var tasks = _runningTasks.ToArray();
            Log(LogLevel.Information, "Waiting for {Count} running event handlers to complete before shutdown...", [tasks.Length]);

            // 最多等待 1.5 秒
            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken);
            var whenAllTask = Task.WhenAll(tasks);

            var completedTask = await Task.WhenAny(whenAllTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Log(LogLevel.Warning, "Shutdown timeout reached. Some event handlers may be terminated abruptly.");
            }
            else
            {
                Log(LogLevel.Information, "All running event handlers completed.");
            }
        }

        await base.StopAsync(cancellationToken);
    }
}
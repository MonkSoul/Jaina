﻿// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Jaina is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Jaina.EventBus;

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
    private readonly ILogger<EventBusHostedService> _logger;

    /// <summary>
    /// 事件源存储器
    /// </summary>
    private readonly IEventSourceStorer _eventSourceStorer;

    /// <summary>
    /// 事件处理程序集合
    /// </summary>
    private readonly ConcurrentDictionary<EventHandlerWrapper, EventHandlerWrapper> _eventHandlers = new();

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
    private bool FuzzyMatch { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志对象</param>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="eventSourceStorer">事件源存储器</param>
    /// <param name="eventSubscribers">事件订阅者集合</param>
    /// <param name="useUtcTimestamp">是否使用 Utc 时间</param>
    /// <param name="fuzzyMatch">是否启用模糊匹配事件消息</param>
    public EventBusHostedService(ILogger<EventBusHostedService> logger
        , IServiceProvider serviceProvider
        , IEventSourceStorer eventSourceStorer
        , IEnumerable<IEventSubscriber> eventSubscribers
        , bool useUtcTimestamp
        , bool fuzzyMatch)
    {
        _logger = logger;
        _eventSourceStorer = eventSourceStorer;
        Monitor = serviceProvider.GetService<IEventHandlerMonitor>();
        Executor = serviceProvider.GetService<IEventHandlerExecutor>();
        UseUtcTimestamp = useUtcTimestamp;
        FuzzyMatch = fuzzyMatch;

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
                var handler = eventHandlerMethod.CreateDelegate<Func<EventHandlerExecutingContext, Task>>(eventSubscriber);

                // 处理同一个事件处理程序支持多个事件 Id 情况
                var eventSubscribeAttributes = eventHandlerMethod.GetCustomAttributes<EventSubscribeAttribute>(false);

                // 逐条包装并添加到 _eventHandlers 集合中
                foreach (var eventSubscribeAttribute in eventSubscribeAttributes)
                {
                    var wrapper = new EventHandlerWrapper(eventSubscribeAttribute.EventId)
                    {
                        Handler = handler,
                        HandlerMethod = eventHandlerMethod,
                        Attribute = eventSubscribeAttribute,
                        Pattern = CheckFuzzyMatch(eventSubscribeAttribute.FuzzyMatch) ? new Regex(eventSubscribeAttribute.EventId, RegexOptions.Singleline) : default
                    };

                    _eventHandlers.TryAdd(wrapper, wrapper);
                }
            }
        }
    }

    /// <summary>
    /// 执行后台任务
    /// </summary>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventBus Hosted Service is running.");

        // 注册后台主机服务停止监听
        stoppingToken.Register(() =>
            _logger.LogDebug($"EventBus Hosted Service is stopping."));

        // 监听服务是否取消
        while (!stoppingToken.IsCancellationRequested)
        {
            // 执行具体任务
            await BackgroundProcessing(stoppingToken);
        }

        _logger.LogCritical($"EventBus Hosted Service is stopped.");
    }

    /// <summary>
    /// 后台调用事件处理程序
    /// </summary>
    /// <param name="stoppingToken">后台主机服务停止时取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        // 从事件存储器中读取一条
        var eventSource = await _eventSourceStorer.ReadAsync(stoppingToken);

        // 处理动态新增/删除事件订阅器
        if (eventSource is EventSubscribeOperateSource subscribeOperateSource)
        {
            ManageEventSubscribers(subscribeOperateSource);

            return;
        }

        // 空检查
        if (string.IsNullOrWhiteSpace(eventSource?.EventId))
        {
            _logger.LogWarning("Invalid EventId, EventId cannot be <null> or an empty string.");

            return;
        }

        // 查找事件 Id 匹配的事件处理程序
        var eventHandlersThatShouldRun = _eventHandlers.Keys.Where(t => t.ShouldRun(eventSource.EventId));

        // 空订阅
        if (!eventHandlersThatShouldRun.Any())
        {
            _logger.LogWarning("Subscriber with event ID <{EventId}> was not found.", eventSource.EventId);

            return;
        }

        // 创建一个任务工厂并保证执行任务都使用当前的计划程序
        var taskFactory = new TaskFactory(System.Threading.Tasks.TaskScheduler.Current);

        // 逐条创建新线程调用
        foreach (var eventHandlerThatShouldRun in eventHandlersThatShouldRun)
        {
            // 创建新的线程执行
            await taskFactory.StartNew(async () =>
            {
                // 创建共享上下文数据对象
                var properties = new Dictionary<object, object>();

                // 获取特性信息，可能为 null
                var eventSubscribeAttribute = eventHandlerThatShouldRun.Attribute;

                // 创建执行前上下文
                var eventHandlerExecutingContext = new EventHandlerExecutingContext(eventSource, properties, eventHandlerThatShouldRun.HandlerMethod, eventSubscribeAttribute)
                {
                    ExecutingTime = UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now
                };

                // 执行异常对象
                InvalidOperationException executionException = default;

                try
                {
                    // 处理任务取消
                    if (eventSource.CancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }

                    // 调用执行前监视器
                    if (Monitor != default)
                    {
                        await Monitor.OnExecutingAsync(eventHandlerExecutingContext);
                    }

                    // 判断是否自定义了执行器
                    if (Executor == default)
                    {
                        // 运行重试
                        await Retry.InvokeAsync(async () =>
                        {
                            await eventHandlerThatShouldRun.Handler!(eventHandlerExecutingContext);
                        }, eventSubscribeAttribute?.NumRetries ?? 0, eventSubscribeAttribute?.RetryTimeout ?? 1000, exceptionTypes: eventSubscribeAttribute?.ExceptionTypes);
                    }
                    else
                    {
                        await Executor.ExecuteAsync(eventHandlerExecutingContext, eventHandlerThatShouldRun.Handler!);
                    }
                }
                catch (Exception ex)
                {
                    // 输出异常日志
                    _logger.LogError(ex, "Error occurred executing {EventId}.", eventSource.EventId);

                    // 标记异常
                    executionException = new InvalidOperationException(string.Format("Error occurred executing {0}.", eventSource.EventId), ex);

                    // 捕获 Task 任务异常信息并统计所有异常
                    if (UnobservedTaskException != default)
                    {
                        var args = new UnobservedTaskExceptionEventArgs(
                            ex as AggregateException ?? new AggregateException(ex));

                        UnobservedTaskException.Invoke(this, args);
                    }
                }
                finally
                {
                    // 调用执行后监视器
                    if (Monitor != default)
                    {
                        // 创建执行后上下文
                        var eventHandlerExecutedContext = new EventHandlerExecutedContext(eventSource, properties, eventHandlerThatShouldRun.HandlerMethod, eventSubscribeAttribute)
                        {
                            ExecutedTime = UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now,
                            Exception = executionException
                        };

                        await Monitor.OnExecutedAsync(eventHandlerExecutedContext);
                    }
                }
            }, stoppingToken);
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

        // 处理新增
        if (subscribeOperateSource.Operate == EventSubscribeOperates.Append)
        {
            var wrapper = new EventHandlerWrapper(eventId)
            {
                Attribute = subscribeOperateSource.Attribute,
                HandlerMethod = subscribeOperateSource.HandlerMethod,
                Handler = subscribeOperateSource.Handler,
                Pattern = CheckFuzzyMatch(subscribeOperateSource.Attribute?.FuzzyMatch) ? new Regex(eventId, RegexOptions.Singleline) : default
            };

            // 追加到集合中
            _eventHandlers.TryAdd(wrapper, wrapper);
        }
        // 处理删除
        else if (subscribeOperateSource.Operate == EventSubscribeOperates.Remove)
        {
            // 删除所有匹配事件 Id 的处理程序
            foreach (var wrapper in _eventHandlers.Keys)
            {
                if (wrapper.EventId == eventId) _eventHandlers.TryRemove(wrapper, out _);
            }
        }
    }


    /// <summary>
    /// 检查是否开启模糊匹配事件 Id 功能
    /// </summary>
    /// <param name="fuzzyMatch"></param>
    /// <returns></returns>
    private bool CheckFuzzyMatch(object fuzzyMatch)
    {
        return fuzzyMatch == null
            ? FuzzyMatch
            : Convert.ToBoolean(fuzzyMatch);
    }
}
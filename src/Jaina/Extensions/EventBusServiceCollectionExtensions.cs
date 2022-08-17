// Copyright (c) 2020-2022 ��Сɮ, Baiqian Co.,Ltd.
// Jaina is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using Jaina.EventBus;
using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// EventBus ģ�������չ
/// </summary>
public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// ��� EventBus ģ��ע��
    /// </summary>
    /// <param name="services">���񼯺϶���</param>
    /// <param name="configureOptionsBuilder">�¼���������ѡ�����ί��</param>
    /// <returns>���񼯺�ʵ��</returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services, Action<EventBusOptionsBuilder> configureOptionsBuilder)
    {
        // ������ʼ�¼���������ѡ�����
        var eventBusOptionsBuilder = new EventBusOptionsBuilder();
        configureOptionsBuilder.Invoke(eventBusOptionsBuilder);

        return services.AddEventBus(eventBusOptionsBuilder);
    }

    /// <summary>
    /// ��� EventBus ģ��ע��
    /// </summary>
    /// <param name="services">���񼯺϶���</param>
    /// <param name="eventBusOptionsBuilder">�¼���������ѡ�����</param>
    /// <returns>���񼯺�ʵ��</returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services, EventBusOptionsBuilder eventBusOptionsBuilder = default)
    {
        // ��ʼ���¼�����������
        eventBusOptionsBuilder ??= new EventBusOptionsBuilder();

        // ע���ڲ�����
        services.AddInternalService(eventBusOptionsBuilder);

        // ͨ������ģʽ����
        services.AddHostedService(serviceProvider =>
        {
            // �����¼����ߺ�̨�������
            var eventBusHostedService = ActivatorUtilities.CreateInstance<EventBusHostedService>(serviceProvider, eventBusOptionsBuilder.UseUtcTimestamp);

            // ����δ��������쳣�¼�
            var unobservedTaskExceptionHandler = eventBusOptionsBuilder.UnobservedTaskExceptionHandler;
            if (unobservedTaskExceptionHandler != default)
            {
                eventBusHostedService.UnobservedTaskException += unobservedTaskExceptionHandler;
            }

            return eventBusHostedService;
        });

        // �����¼����߷���
        eventBusOptionsBuilder.Build(services);

        return services;
    }

    /// <summary>
    /// ע���ڲ�����
    /// </summary>
    /// <param name="services">���񼯺϶���</param>
    /// <param name="eventBusOptions">�¼���������ѡ��</param>
    /// <returns>���񼯺�ʵ��</returns>
    private static IServiceCollection AddInternalService(this IServiceCollection services, EventBusOptionsBuilder eventBusOptions)
    {
        // ����Ĭ���ڴ�ͨ���¼�Դ����
        var defaultStorerOfChannel = new ChannelEventSourceStorer(eventBusOptions.ChannelCapacity);

        // ע���̨������нӿ�/ʵ��Ϊ���������ù�����ʽ����
        services.AddSingleton<IEventSourceStorer>(_ =>
        {
            return defaultStorerOfChannel;
        });

        // ע��Ĭ���ڴ�ͨ���¼�������
        services.AddSingleton<IEventPublisher, ChannelEventPublisher>();

        return services;
    }
}
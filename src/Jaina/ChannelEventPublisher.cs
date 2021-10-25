// Copyright (c) 2020-2021 百小僧, Baiqian Co.,Ltd.
// Jaina is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using System.Threading.Tasks;

namespace Jaina.EventBus
{
    /// <summary>
    /// 基于内存通道事件发布者（默认实现）
    /// </summary>
    internal sealed partial class ChannelEventPublisher : IEventPublisher
    {
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

        /// <summary>
        /// 发布一条消息
        /// </summary>
        /// <param name="eventSource">事件源</param>
        /// <returns><see cref="Task"/> 实例</returns>
        public async Task PublishAsync(IEventSource eventSource)
        {
            await _eventSourceStorer.WriteAsync(eventSource, eventSource.CancellationToken);
        }
    }
}
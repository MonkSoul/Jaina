// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Jaina is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

namespace Jaina.EventBus;

/// <summary>
/// 事件订阅器操作选项
/// </summary>
/// <remarks>控制动态新增/删除事件订阅器</remarks>
internal enum EventSubscribeOperates
{
    /// <summary>
    /// 添加一条订阅器
    /// </summary>
    Append,

    /// <summary>
    /// 删除一条订阅器
    /// </summary>
    Remove
}
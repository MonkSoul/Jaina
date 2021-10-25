# Jaina

[![license](https://img.shields.io/badge/license-MulanPSL--2.0-orange?cacheSeconds=10800)](https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE) [![nuget](https://img.shields.io/nuget/v/Jaina.svg?cacheSeconds=10800)](https://www.nuget.org/packages/Jaina) [![dotNET China](https://img.shields.io/badge/organization-dotNET%20China-yellow?cacheSeconds=10800)](https://gitee.com/dotnetchina)

一个应用程序框架，您可以将它集成到任何 .NET/C# 应用程序中。

## 安装

- [Package Manager](https://www.nuget.org/packages/Jaina)

```powershell
Install-Package Jaina
```

- [.NET CLI](https://www.nuget.org/packages/Jaina)

```powershell
dotnet add package Jaina
```

## 例子

我们在[主页](https://gitee.com/dotnetchina/Jaina)上有不少例子，这是让您入门的第一个：

### 快速入门

1. 定义事件订阅者 `ToDoEventSubscriber`：

```cs
using Jaina.EventBus;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Jaina.Samples
{
    // 实现 IEventSubscriber 接口
    public class ToDoEventSubscriber : IEventSubscriber
    {
        private readonly ILogger<ToDoEventSubscriber> _logger;
        public ToDoEventSubscriber(ILogger<ToDoEventSubscriber> logger)
        {
            _logger = logger;
        }

        // 标记 [EventSubscribe(事件 Id)] 特性
        [EventSubscribe("ToDo:Create")]
        public async Task CreateToDo(EventHandlerExecutingContext context)
        {
            var todo = context.Source;

            _logger.LogInformation("创建一个 ToDo：{Name}", todo.Payload);
            await Task.CompletedTask;
        }
    }
}
```

2. 创建控制器 `ToDoController`，依赖注入 `IEventPublisher` 服务：

```cs
using Jaina.EventBus;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Jaina.Samples.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ToDoController : ControllerBase
    {
        // 依赖注入事件发布者 IEventPublisher
        private readonly IEventPublisher _eventPublisher;
        public ToDoController(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        // 发布 ToDo:Create 消息
        [HttpPost]
        public async Task CreateDoTo(string name)
        {
            await _eventPublisher.PublishAsync(new ChannelEventSource("ToDo:Create", name));
        }
    }
}
```

3. 在 `Startup.cs` 注册 `EventBus` 服务：

```cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jaina.Samples
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // 注册 EventBus 服务
            services.AddEventBus(buidler =>
            {
                // 注册 ToDo 事件订阅者
                buidler.AddSubscriber<ToDoEventSubscriber>();
            });

            // ....
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ....
        }
    }
}
```

4. 运行项目：

```log
info: Jaina.Samples.ToDoEventSubscriber[0]
      创建一个 ToDo：Jaina
```

## 文档

您可以在[主页](https://dotnetchina.gitee.io/Jaina)找到 Jaina 文档。

## 贡献

该存储库的主要目的是继续发展 Jaina 核心，使其更快、更易于使用。Jaina 的开发在 [Gitee](https://gitee.com/dotnetchina/Jaina) 上公开进行，我们感谢社区贡献错误修复和改进。

## 许可证

Jaina 采用 [MulanPSL-2.0](https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE) 开源许可证。

```
Copyright (c) 2020-2021 百小僧, Baiqian Co.,Ltd.
Jaina is licensed under Mulan PSL v2.
You can use this software according to the terms andconditions of the Mulan PSL v2.
You may obtain a copy of Mulan PSL v2 at:
            https://gitee.com/dotnetchina/Jaina/blob/master/LICENSE
THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUTWARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED,INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT,MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
See the Mulan PSL v2 for more details.
```

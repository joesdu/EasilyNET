### EasilyNET

[![LICENSE](https://img.shields.io/github/license/joesdu/Hoyo)](https://img.shields.io/github/license/joesdu/Hoyo)
[![ISSUES](https://img.shields.io/github/issues/joesdu/Hoyo)](https://img.shields.io/github/issues/joesdu/Hoyo)
[![FORKS](https://img.shields.io/github/forks/joesdu/Hoyo)](https://img.shields.io/github/forks/joesdu/Hoyo)
[![STARS](https://img.shields.io/github/stars/joesdu/Hoyo)](https://img.shields.io/github/stars/joesdu/Hoyo)
![GitHub commit activity](https://img.shields.io/github/commit-activity/y/joesdu/Hoyo)
![GitHub last commit](https://img.shields.io/github/last-commit/joesdu/Hoyo)

EasilyNET Packages

- Simplifying the use of MongoDB drivers
- Some common tool extensions
- ABP-like framework for automatic module injection
- Mongo Storage support for IdentityServer 6
- MongoDB GridFS usage simplification support.
- Integration of some common filters, middleware in WebApi

### Supported Versions

| Version           | Supported          |
| ----------------- | ------------------ |
| .NET 8            | :white_check_mark: |
| .NET 7            | :white_check_mark: |
| .NET 6            | :white_check_mark: |
| .NET 5            | :x:                |
| .NET Core 3.x     | :x:                |
| .NET Standard 2.0 | :white_check_mark: |
| .NET Standard 2.1 | :white_check_mark: |

| NuGet Package                                                                                                 | Version                                                                        | Download                                                                        | Description                                                           |
| ------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ | ------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| [EasilyNET.AutoDependencyInjection](https://www.nuget.org/packages/EasilyNET.AutoDependencyInjection)         | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.AutoDependencyInjection)     | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.AutoDependencyInjection)     | 类似于 ABP 模块化自动注入服务的一个工具包                                  |
| [EasilyNET.RabbitBus](https://www.nuget.org/packages/EasilyNET.RabbitBus)                                     | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.RabbitBus)                   | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.RabbitBus)                   | 基于 RabbitMQ 的消息总线处理方案                                         |
| [EasilyNET.Extensions](https://www.nuget.org/packages/EasilyNET.Extensions)                                   | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Extensions)                  | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Extensions)                  | 一些扩展包,用于支持一些各种扩展方法和奇妙语法功能                            |
| [EasilyNET.Images](https://www.nuget.org/packages/EasilyNET.Images)                                           | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Images)                      | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Images)                      | 一些涉及到图形的工具包,目前仅有 QrCode                                    |
| [EasilyNET.IdentityServer.MongoStorage](https://www.nuget.org/packages/EasilyNET.IdentityServer.MongoStorage) | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.IdentityServer.MongoStorage) | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.IdentityServer.MongoStorage) | IDS6.x 的 Mongodb 持久化支持方案                                        |
| [EasilyNET.Mongo](https://www.nuget.org/packages/EasilyNET.Mongo)                                             | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo)                       | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo)                       | MongoDB 的驱动扩展                                                     |
| [EasilyNET.Mongo.ConsoleDebug](https://www.nuget.org/packages/EasilyNET.Mongo.ConsoleDebug)                   | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo.ConsoleDebug)          | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo.ConsoleDebug)          | MongoDB 的执行命令输出到控制台                                           |
| [EasilyNET.Mongo.Extension](https://www.nuget.org/packages/EasilyNET.Mongo.Extension)                         | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo.Extension)             | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo.Extension)             | MongoDB 的类型扩展,以及自定义类型扩展方案                                  |
| [EasilyNET.Mongo.GridFS](https://www.nuget.org/packages/EasilyNET.Mongo.GridFS)                               | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo.GridFS)                | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo.GridFS)                | MongoDB GridFS 对象存储解决方案,使对象存储操作简便                         |
| [EasilyNET.Mongo.GridFS.Extension](https://www.nuget.org/packages/EasilyNET.Mongo.GridFS.Extension)           | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo.GridFS.Extension)      | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo.GridFS.Extension)      | EasilyNET.Mongo.GridFS 扩展,添加虚拟文件系统,便于文件在线查看              |
| [EasilyNET.Tools](https://www.nuget.org/packages/EasilyNET.Tools)                                             | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Tools)                       | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Tools)                       | 一些工具包,如 RMB 大写,农历,身份证号码校验等                               |
| [EasilyNET.WebCore](https://www.nuget.org/packages/EasilyNET.WebCore)                                         | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.WebCore)                     | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.WebCore)                     | 提供 Swagger 的一些 Filtter,以及 JsonConverter,和全局统一返回和异常处理支持 |

![微信公众号](https://github.com/joesdu/joesdu/blob/main/wechat-official-account.png#pic_center)

## How to participate in this project

- First Fork the project to your own repository.
- Then create a branch of your own, name it whatever you want, such as your nickname, or the name of the feature you are working on.
- Then commit to your own repository.
- Then go to this project and create Pull Requests.
- Wait for the administrator to merge the project and then delete your own repository Fork
### EasilyNET

**注意:** 由于本项目一般会试用和支持最新版本的.NET SDK,所以为了保证你能正常编译,请确保运行之前安装当前最新版本的 SDK 预览版,如现在为: .NET8 preview-4

[![LICENSE](https://img.shields.io/github/license/EasilyNET/EasilyNET)](https://img.shields.io/github/license/EasilyNET/EasilyNET)
[![ISSUES](https://img.shields.io/github/issues/EasilyNET/EasilyNET)](https://img.shields.io/github/issues/EasilyNET/EasilyNET)
[![FORKS](https://img.shields.io/github/forks/EasilyNET/EasilyNET)](https://img.shields.io/github/forks/EasilyNET/EasilyNET)
[![STARS](https://img.shields.io/github/stars/EasilyNET/EasilyNET)](https://img.shields.io/github/stars/EasilyNET/EasilyNET)
![GitHub commit activity](https://img.shields.io/github/commit-activity/y/EasilyNET/EasilyNET)
![GitHub last commit](https://img.shields.io/github/last-commit/EasilyNET/EasilyNET)

EasilyNET Packages

- simple qrcode
- eventbus with rabbitmq
- simplifying the use of mongodb drivers
- some common tool extensions
- automatic module injection
- mongodb adds (dynamic|object) serialization support (mongodb.driver 2.19.0+ onwards has removed it)
- mongodb storage support for identityserver 6
- mongodb gridfs usage simplification support
- integration of some common filters, middleware in webapi

| NuGet Package                                                                                                 | Version                                                                        | Download                                                                        | Description                                                                    |
| ------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ | ------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| [EasilyNET.AutoDependencyInjection](https://www.nuget.org/packages/EasilyNET.AutoDependencyInjection)         | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.AutoDependencyInjection)     | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.AutoDependencyInjection)     | 模块化自动注入服务                                                             |
| [EasilyNET.RabbitBus.AspNetCore](https://www.nuget.org/packages/EasilyNET.RabbitBus.AspNetCore)               | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.RabbitBus.AspNetCore)        | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.RabbitBus.AspNetCore)        | 基于 RabbitMQ 的消息总线处理方案                                               |
| [EasilyNET.Core](https://www.nuget.org/packages/EasilyNET.Core)                                               | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Core)                        | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Core)                        | 核心库等,用于支持一些各种扩展方法和奇妙语法功能,RMB 大写,农历,身份证号码校验等 |
| [EasilyNET.Images](https://www.nuget.org/packages/EasilyNET.Images)                                           | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Images)                      | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Images)                      | 一些涉及到图形的工具包,目前仅有 QrCode                                         |
| [EasilyNET.IdentityServer.MongoStorage](https://www.nuget.org/packages/EasilyNET.IdentityServer.MongoStorage) | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.IdentityServer.MongoStorage) | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.IdentityServer.MongoStorage) | IDS6.x 的 Mongodb 持久化支持方案                                               |
| [EasilyNET.Mongo.AspNetCore](https://www.nuget.org/packages/EasilyNET.Mongo.AspNetCore)                       | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo.AspNetCore)            | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo.AspNetCore)            | MongoDB 驱动扩展                                                             |
| [EasilyNET.Mongo.Core](https://www.nuget.org/packages/EasilyNET.Mongo.Core)                                   | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo.Core)                  | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo.Core)                  | MongoDB 驱动基础类型支持,便于将业务代码和配置代码分开                           |
| [EasilyNET.Mongo.ConsoleDebug](https://www.nuget.org/packages/EasilyNET.Mongo.ConsoleDebug)                   | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo.ConsoleDebug)          | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo.ConsoleDebug)          | MongoDB 的执行命令输出到控制台                                                 |
| [EasilyNET.MongoSerializer.AspNetCore](https://www.nuget.org/packages/EasilyNET.MongoSerializer.AspNetCore)   | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.MongoSerializer.AspNetCore)  | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.MongoSerializer.AspNetCore)  | MongoDB 的类型扩展,以及自定义类型扩展方案                                      |
| [EasilyNET.MongoGridFS.AspNetCore](https://www.nuget.org/packages/EasilyNET.MongoGridFS.AspNetCore)           | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.MongoGridFS.AspNetCore)      | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.MongoGridFS.AspNetCore)      | MongoDB GridFS 对象存储解决方案,使对象存储操作简便                             |
| [EasilyNET.WebCore](https://www.nuget.org/packages/EasilyNET.WebCore)                                         | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.WebCore)                     | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.WebCore)                     | 提供 JsonConverter,和全局统一返回和异常处理支持,以及一些 WebAPI 常用的东西     |
| [EasilyNET.WebCore.Swagger](https://www.nuget.org/packages/EasilyNET.WebCore.Swagger)                         | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.WebCore.Swagger)             | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.WebCore.Swagger)             | 提供 Swagger 的一些 Filter 实现.                                               |
| [EasilyNET.Security](https://www.nuget.org/packages/EasilyNET.Security)                                       | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Security)                    | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Security)                    | 一个常用加密算法的封装库,从使用简单的目的出发                                  |

![微信公众号](https://github.com/joesdu/joesdu/blob/main/wechat-official-account.png#pic_center)

## How to participate in this project

- fork the project to your own repository.
- then create a branch of your own, name it whatever you want, such as your nickname, or the name of the feature you are working on.
- then commit to your own repository.
- then go to this project and create pull requests.
- wait for the administrator to merge the project and then delete your own repository fork.

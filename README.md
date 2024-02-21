### EasilyNET

**寻求帮助**: 希望能有懂 Razor 的同学帮我根据官方的 QuickUI 写一个管理页面, 若是能教我 Razor 的话那就更 Nice 了.

**注意:** 由于本项目一般会试用和支持最新版本的.NET SDK,所以为了保证你能正常编译,请确保运行之前安装当前最新版本的 SDK

**解决 git 不区分大小**

```bash
git config core.ignorecase false
```

[![LICENSE](https://img.shields.io/github/license/EasilyNET/EasilyNET)](https://img.shields.io/github/license/EasilyNET/EasilyNET) [![ISSUES](https://img.shields.io/github/issues/EasilyNET/EasilyNET)](https://img.shields.io/github/issues/EasilyNET/EasilyNET) [![FORKS](https://img.shields.io/github/forks/EasilyNET/EasilyNET)](https://img.shields.io/github/forks/EasilyNET/EasilyNET) [![STARS](https://img.shields.io/github/stars/EasilyNET/EasilyNET)](https://img.shields.io/github/stars/EasilyNET/EasilyNET) ![GitHub commit activity](https://img.shields.io/github/commit-activity/y/EasilyNET/EasilyNET) ![GitHub last commit](https://img.shields.io/github/last-commit/EasilyNET/EasilyNET)

EasilyNET Packages

- AES,DES,RC4,TripleDES,RSA,SM2,SM3,SM4 加密,验签等算法
- 基于 MongoDB 的分布式锁
- 简单的二维码生成和读取
- 基于 RabbitMQ 的消息总线实现
- 对 MongoDB 驱动的一些封装,方便使用以及一些常用的默认配置
- 雪花 ID,一些常用的数据类型,枚举,扩展方法等
- 自动模块化注入服务
- MongoDB 添加 DateOnly 和 TimeOnly 的支持
- IdentityServer 7.x 的 MongoDB 持久化支持, 6.x 随着.NET6 和 7 的移除不再支持.
- MongoDB GridFS 用法的简单支持(常用用法)和使用案例.
- 在 WebAPI 中集成一些常见的过滤器和中间件
- 对 Swagger 文档添加分组,隐藏 API 和添加部分数据类型默认值显示的支持,方便前端工程师查阅
- 对 MongoDB 执行命令添加个性化输出.(EasilyNET.Mongo.ConsoleDebug)

#### Core

| NuGet Package                                                                         | Version                                                            | Download                                                            | Description                                       |
|---------------------------------------------------------------------------------------|--------------------------------------------------------------------|---------------------------------------------------------------------|---------------------------------------------------|
| [EasilyNET.Core](https://www.nuget.org/packages/EasilyNET.Core)                       | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Core)            | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Core)            | 核心库等,用于支持一些各种扩展方法和奇妙语法功能,RMB 大写,农历,身份证号码校验等       |
| [EasilyNET.WebCore](https://www.nuget.org/packages/EasilyNET.WebCore)                 | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.WebCore)         | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.WebCore)         | 提供 JsonConverter,和全局统一返回和异常处理支持,以及一些 WebAPI 常用的东西 |
| [EasilyNET.WebCore.Swagger](https://www.nuget.org/packages/EasilyNET.WebCore.Swagger) | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.WebCore.Swagger) | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.WebCore.Swagger) | 提供 Swagger 的一些 Filter 实现.                         |

#### Framework

| NuGet Package                                                                                         | Version                                                                    | Download                                                                    | Description                           |
|-------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------|-----------------------------------------------------------------------------|---------------------------------------|
| [EasilyNET.AutoDependencyInjection](https://www.nuget.org/packages/EasilyNET.AutoDependencyInjection) | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.AutoDependencyInjection) | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.AutoDependencyInjection) | 模块化自动注入服务,特性和接口注入等多种方式                |
| [EasilyNET.Images](https://www.nuget.org/packages/EasilyNET.Images)                                   | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Images)                  | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Images)                  | 一些涉及到图形的工具包,目前仅有 QrCode               |
| [EasilyNET.RabbitBus.AspNetCore](https://www.nuget.org/packages/EasilyNET.RabbitBus.AspNetCore)       | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.RabbitBus.AspNetCore)    | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.RabbitBus.AspNetCore)    | 基于 RabbitMQ 的消息总线                     |
| [EasilyNET.Security](https://www.nuget.org/packages/EasilyNET.Security)                               | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Security)                | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Security)                | AES,DES,RC4,TripleDES,RSA,SM2,SM3,SM4 |

#### Mongo

| NuGet Package                                                                                                         | Version                                                                            | Download                                                                            | Description                   |
|-----------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|-------------------------------|
| [EasilyNET.IdentityServer.MongoStorage](https://www.nuget.org/packages/EasilyNET.IdentityServer.MongoStorage)         | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.IdentityServer.MongoStorage)     | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.IdentityServer.MongoStorage)     | IDS7.x 的 Mongodb 持久化          |
| [EasilyNET.Mongo.AspNetCore](https://www.nuget.org/packages/EasilyNET.Mongo.AspNetCore)                               | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo.AspNetCore)                | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo.AspNetCore)                | MongoDB 驱动扩展                  |
| [EasilyNET.Mongo.ConsoleDebug](https://www.nuget.org/packages/EasilyNET.Mongo.ConsoleDebug)                           | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.Mongo.ConsoleDebug)              | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.Mongo.ConsoleDebug)              | MongoDB 的执行命令输出到控制台           |
| [EasilyNET.MongoDistributedLock](https://www.nuget.org/packages/EasilyNET.MongoDistributedLock)                       | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.MongoDistributedLock)            | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.MongoDistributedLock)            | 基于 MongoDB 的分布式锁              |
| [EasilyNET.MongoDistributedLock.AspNetCore](https://www.nuget.org/packages/EasilyNET.MongoDistributedLock.AspNetCore) | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.MongoDistributedLock.AspNetCore) | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.MongoDistributedLock.AspNetCore) | 基于 MongoDB 的分布式锁              |
| [EasilyNET.MongoGridFS.AspNetCore](https://www.nuget.org/packages/EasilyNET.MongoGridFS.AspNetCore)                   | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.MongoGridFS.AspNetCore)          | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.MongoGridFS.AspNetCore)          | MongoDB GridFS 对象存储,使对象存储操作简便 |
| [EasilyNET.MongoSerializer.AspNetCore](https://www.nuget.org/packages/EasilyNET.MongoSerializer.AspNetCore)           | ![Nuget](https://img.shields.io/nuget/v/EasilyNET.MongoSerializer.AspNetCore)      | ![Nuget](https://img.shields.io/nuget/dt/EasilyNET.MongoSerializer.AspNetCore)      | MongoDB 的类型扩展,以及自定义类型扩展       |

#### 感谢 [JetBrains](https://www.jetbrains.com/shop/eform/opensource) 对本项目的支持!

<img src="https://www.jetbrains.com/shop/static/images/jetbrains-logo-inv.svg" height="100">

## 如何为本项目做出贡献

- Fork 本项目到你自己的仓库.
- 创建一个属于你自己的分支,名字随便你怎么取.
- 然后提交代码到你自己仓库的分支上.
- 然后到本项目创建一个 PR.
- 等待管理员合并 PR 后即可删除掉你自己的仓库.

### Git 贡献提交规范

- 使用 Emoji [参考](https://gitmoji.dev)

|  符号   | 代码                          | 使用场景                  |
|:-----:|-----------------------------|-----------------------|
|  🎨   | :art:                       | 改进代码的结构/格式            |
|  ⚡️   | :zap:                       | 提高性能                  |
|  🔥   | :fire:                      | 删除代码或文件               |
|  🐛   | :bug:                       | 修复错误                  |
|  🚑️  | :ambulance:                 | 关键修补程序                |
|   ✨   | :sparkles:                  | 引入新功能                 |
|  📝   | :memo:                      | 添加或更新文档               |
|  🚀   | :rocket:                    | 部署内容                  |
|  💄   | :lipstick:                  | 添加或更新 UI 和样式文件        |
|  🎉   | :tada:                      | 开始一个项目                |
|   ✅   | :white_check_mark:          | 添加、更新或通过测试            |
|  🔒️  | :lock:                      | 修复安全问题                |
|  🔐   | :closed_lock_with_key:      | 添加或更新机密               |
|  🔖   | :bookmark:                  | 发布/版本标签               |
|  🚨   | :rotating_light:            | 修复编译器/林特警告            |
|  🚧   | :construction:              | 工作正在进行中               |
|  💚   | :green_heart:               | 修复 CI 生成              |
|  ⬇️   | :arrow_down:                | 降级依赖项                 |
|  ⬆️   | :arrow_up:                  | 升级依赖项                 |
|  📌   | :pushpin:                   | 将依赖项固定到特定版本           |
|  👷   | :construction_worker:       | 添加或更新 CI 生成系统         |
|  📈   | :chart_with_upwards_trend:  | 添加或更新分析或跟踪代码          |
|  ♻️   | :recycle:                   | 重构代码                  |
|   ➕   | :heavy_plus_sign:           | 添加依赖项                 |
|   ➖   | :heavy_minus_sign:          | 删除依赖项                 |
|  🔧   | :wrench:                    | 添加或更新配置文件             |
|  🔨   | :hammer:                    | 添加或更新开发脚本             |
|  🌐   | :globe_with_meridians:      | 国际化和本地化               |
|  ✏️   | :pencil2:                   | 修复拼写错误                |
|  💩   | :poop:                      | 编写需要改进的不良代码           |
|  ⏪️   | :rewind:                    | 还原更改                  |
|  🔀   | :twisted_rightwards_arrows: | 合并分支                  |
|  📦️  | :package:                   | 添加或更新已编译的文件或包         |
|  👽️  | :alien:                     | 由于外部 API 更改而更新代码      |
|  🚚   | :truck:                     | 移动或重命名资源(例如:文件,路径,路由) |
|  📄   | :page_facing_up:            | 添加或更新许可证              |
|  💥   | :boom:                      | 引入重大更改                |
|  🍱   | :bento:                     | 添加或更新资产               |
|  ♿️   | :wheelchair:                | 提高可访问性                |
|  💡   | :bulb:                      | 在源代码中添加或更新注释          |
|  🍻   | :beers:                     | 醉醺醺地编写代码              |
|  💬   | :speech_balloon:            | 添加或更新文本和文字            |
|  🗃️  | :card_file_box:             | 执行与数据库相关的更改           |
|  🔊   | :loud_sound:                | 添加或更新日志               |
|  🔇   | :mute:                      | 删除日志                  |
|  👥   | :busts_in_silhouette:       | 添加或更新参与者              |
|  🚸   | :children_crossing:         | 改善用户体验/可用性            |
|  🏗️  | :building_construction:     | 进行体系结构更改              |
|  📱   | :iphone:                    | 致力于响应式设计              |
|  🤡   | :clown_face:                | 嘲笑事物                  |
|  🥚   | :egg:                       | 添加或更新复活节彩蛋            |
|  🙈   | :see_no_evil:               | 添加或更新.gitignore 文件    |
|  📸   | :camera_flash:              | 添加或更新快照               |
|  ⚗️   | :alembic:                   | 执行实验                  |
|  🔍️  | :mag:                       | 改进搜索引擎优化              |
|  🏷️  | :label:                     | 添加或更新类型               |
|  🌱   | :seedling:                  | 添加或更新种子文件             |
|  🚩   | :triangular_flag_on_post:   | 添加、更新或删除功能标志          |
|  🥅   | :goal_net:                  | 捕获错误                  |
|  💫   | :dizzy:                     | 添加或更新动画和过渡            |
|  🗑️  | :wastebasket:               | 弃用需要清理的代码             |
|  🛂   | :passport_control:          | 处理与授权、角色和权限相关的代码      |
|  🩹   | :adhesive_bandage:          | 非关键问题的简单修复            |
|  🧐   | :monocle_face:              | 数据探索/检查               |
|  ⚰️   | :coffin:                    | 删除死代码                 |
|  🧪   | :test_tube:                 | 添加失败的测试               |
|  👔   | :necktie:                   | 添加或更新业务逻辑             |
|  🩺   | :stethoscope:               | 添加或更新运行状况检查           |
|  🧱   | :bricks:                    | 与基础结构相关的更改            |
| 🧑‍💻 | :technologist:              | 改善开发人员体验              |

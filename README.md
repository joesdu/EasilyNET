### EasilyNET

[![License][1]][2] ![ISSUES][3] ![FORKS][4] ![STARS][5] ![Commit Activity][6] ![Last Commit][7]

<p align="center">
    <img src="https://repobeats.axiom.co/api/embed/cd2c97db26ee6fe230353beefd5d532448054f0a.svg" />
</p>

**注意:** 由于本项目一般会试用和支持最新版本的.NET SDK,所以为了保证你能正常编译,请确保运行之前安装当前最新版本的 SDK

**解决 git 不区分大小**

```bash
git config core.ignorecase false
```

EasilyNET Packages

- AES,DES,RC4,TripleDES,RSA,SM2,SM3,SM4 加密,验签等算法
- 基于 MongoDB 的分布式锁
- 基于 RabbitMQ 的消息总线实现
- 对 MongoDB 驱动的一些封装,方便使用以及一些常用的默认配置
- 雪花 ID,一些常用的数据类型,枚举,扩展方法等
- 自动模块化注入服务
- MongoDB 添加 DateOnly 和 TimeOnly 的支持
- MongoDB GridFS 用法的简单支持(常用用法)和使用案例.
- 在 WebAPI 中集成一些常见的过滤器和中间件
- 对 Swagger 文档添加分组,隐藏 API 和添加部分数据类型默认值显示的支持,方便前端工程师查阅
- 对 MongoDB 执行命令添加个性化输出.(EasilyNET.Mongo.ConsoleDebug)
- 部分库的[使用案例](./sample/WebApi.Test.Unit/README.md)

#### Core

| NuGet Package                   | Version      | Download     | Document                                          |
| ------------------------------- | ------------ | ------------ | ------------------------------------------------- |
| [EasilyNET.Core][8]             | ![Nuget][9]  | ![Nuget][10] | [文档](./src/EasilyNET.Core/README.md)            |
| [EasilyNET.WebCore][11]         | ![Nuget][12] | ![Nuget][13] | [文档](./src/EasilyNET.WebCore/README.md)         |
| [EasilyNET.WebCore.Swagger][14] | ![Nuget][15] | ![Nuget][16] | [文档](./src/EasilyNET.WebCore.Swagger/README.md) |

#### Framework

| NuGet Package                           | Version      | Download     | Document                                                  |
| --------------------------------------- | ------------ | ------------ | --------------------------------------------------------- |
| [EasilyNET.AutoDependencyInjection][17] | ![Nuget][18] | ![Nuget][19] | [文档](./src/EasilyNET.AutoDependencyInjection/README.md) |
| [EasilyNET.RabbitBus.AspNetCore][23]    | ![Nuget][24] | ![Nuget][25] | [文档](./src/EasilyNET.RabbitBus.AspNetCore/README.md)    |
| [EasilyNET.Security][26]                | ![Nuget][27] | ![Nuget][28] | [文档](./src/EasilyNET.Security/README.md)                |

#### Mongo

| NuGet Package                                   | Version      | Download     | Document                                                          |
| ----------------------------------------------- | ------------ | ------------ | ----------------------------------------------------------------- |
| [EasilyNET.Mongo.AspNetCore][29]                | ![Nuget][30] | ![Nuget][31] | [文档](./src/EasilyNET.Mongo.AspNetCore/README.md)                |
| [EasilyNET.Mongo.ConsoleDebug][32]              | ![Nuget][33] | ![Nuget][34] | [文档](./src/EasilyNET.Mongo.ConsoleDebug/README.md)              |
| [EasilyNET.MongoDistributedLock][35]            | ![Nuget][36] | ![Nuget][37] | [文档](./src/EasilyNET.MongoDistributedLock/README.md)            |
| [EasilyNET.MongoDistributedLock.AspNetCore][38] | ![Nuget][39] | ![Nuget][40] | [文档](./src/EasilyNET.MongoDistributedLock.AspNetCore/README.md) |
| [EasilyNET.MongoGridFS.AspNetCore][41]          | ![Nuget][42] | ![Nuget][43] | [文档](./src/EasilyNET.MongoGridFS.AspNetCore/README.md)          |
| [EasilyNET.MongoSerializer.AspNetCore][44]      | ![Nuget][45] | ![Nuget][46] | [文档](./src/EasilyNET.MongoSerializer.AspNetCore/README.md)      |

#### 感谢 [JetBrains](https://www.jetbrains.com/shop/eform/opensource) 对本项目的支持!

<img src="https://www.jetbrains.com/shop/static/images/jetbrains-logo-inv.svg" height="100">

## 如何为本项目做出贡献

- Fork 本项目到你自己的仓库.
- 创建一个属于你自己的分支,名字随便你怎么取.
- 然后提交代码到你自己仓库的分支上.
- 然后到本项目创建一个 PR.
- 等待管理员合并 PR 后即可删除掉你自己的仓库.

### Git 贡献提交规范

- 使用 Emoji [参考](https://gitmoji.dev) | [Emoji 中文含义](gitemoji.md)

| 代号     | 说明                                                     |
| :------- | -------------------------------------------------------- |
| feat     | 新功能(feature)                                          |
| fix      | 修复 bug,可以是 QA 发现的 BUG,也可以是研发自己发现的 BUG |
| docs     | 文档(documentation)                                      |
| style    | 格式(不影响代码运行的变动)                               |
| refactor | 重构(即不是新增功能,也不是修改 bug 的代码变动)           |
| perf     | 优化相关,比如提升性能、体验                             |
| test     | 增加测试                                                 |
| chore    | 构建过程或辅助工具的变动                                 |
| revert   | 回滚到上一个版本                                         |
| merge    | 代码合并                                                 |
| sync     | 同步主线或分支的 Bug                                     |


<!--文档中的各项链接-->

[1]: https://img.shields.io/badge/License-MIT-brightgreen.svg
[2]: LICENSE
[3]: https://img.shields.io/github/issues/EasilyNET/EasilyNET
[4]: https://img.shields.io/github/forks/EasilyNET/EasilyNET
[5]: https://img.shields.io/github/stars/EasilyNET/EasilyNET
[6]: https://img.shields.io/github/commit-activity/y/EasilyNET/EasilyNET
[7]: https://img.shields.io/github/last-commit/EasilyNET/EasilyNET
[8]: https://www.nuget.org/packages/EasilyNET.Core
[9]: https://img.shields.io/nuget/v/EasilyNET.Core
[10]: https://img.shields.io/nuget/dt/EasilyNET.Core
[11]: https://www.nuget.org/packages/EasilyNET.WebCore
[12]: https://img.shields.io/nuget/v/EasilyNET.WebCore
[13]: https://img.shields.io/nuget/dt/EasilyNET.WebCore
[14]: https://www.nuget.org/packages/EasilyNET.WebCore.Swagger
[15]: https://img.shields.io/nuget/v/EasilyNET.WebCore.Swagger
[16]: https://img.shields.io/nuget/dt/EasilyNET.WebCore.Swagger
[17]: https://www.nuget.org/packages/EasilyNET.AutoDependencyInjection
[18]: https://img.shields.io/nuget/v/EasilyNET.AutoDependencyInjection
[19]: https://img.shields.io/nuget/dt/EasilyNET.AutoDependencyInjection
[23]: https://www.nuget.org/packages/EasilyNET.RabbitBus.AspNetCore
[24]: https://img.shields.io/nuget/v/EasilyNET.RabbitBus.AspNetCore
[25]: https://img.shields.io/nuget/dt/EasilyNET.RabbitBus.AspNetCore
[26]: https://www.nuget.org/packages/EasilyNET.Security
[27]: https://img.shields.io/nuget/v/EasilyNET.Security
[28]: https://img.shields.io/nuget/dt/EasilyNET.Security
[29]: https://www.nuget.org/packages/EasilyNET.Mongo.AspNetCore
[30]: https://img.shields.io/nuget/v/EasilyNET.Mongo.AspNetCore
[31]: https://img.shields.io/nuget/dt/EasilyNET.Mongo.AspNetCore
[32]: https://www.nuget.org/packages/EasilyNET.Mongo.ConsoleDebug
[33]: https://img.shields.io/nuget/v/EasilyNET.Mongo.ConsoleDebug
[34]: https://img.shields.io/nuget/dt/EasilyNET.Mongo.ConsoleDebug
[35]: https://www.nuget.org/packages/EasilyNET.MongoDistributedLock
[36]: https://img.shields.io/nuget/v/EasilyNET.MongoDistributedLock
[37]: https://img.shields.io/nuget/dt/EasilyNET.MongoDistributedLock
[38]: https://www.nuget.org/packages/EasilyNET.MongoDistributedLock.AspNetCore
[39]: https://img.shields.io/nuget/v/EasilyNET.MongoDistributedLock.AspNetCore
[40]: https://img.shields.io/nuget/dt/EasilyNET.MongoDistributedLock.AspNetCore
[41]: https://www.nuget.org/packages/EasilyNET.MongoGridFS.AspNetCore
[42]: https://img.shields.io/nuget/v/EasilyNET.MongoGridFS.AspNetCore
[43]: https://img.shields.io/nuget/dt/EasilyNET.MongoGridFS.AspNetCore
[44]: https://www.nuget.org/packages/EasilyNET.MongoSerializer.AspNetCore
[45]: https://img.shields.io/nuget/v/EasilyNET.MongoSerializer.AspNetCore
[46]: https://img.shields.io/nuget/dt/EasilyNET.MongoSerializer.AspNetCore

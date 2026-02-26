### EasilyNET

[![License][1]][2] ![ISSUES][3] ![FORKS][4] ![STARS][5] ![Commit Activity][6] ![Last Commit][7]

<div style="text-align: center;">
    <img alt="Welcome EasilyNET" src="https://repobeats.axiom.co/api/embed/cd2c97db26ee6fe230353beefd5d532448054f0a.svg" />
</div>

**注意:** 本项目依赖最新版本的 .NET SDK(使用预览特性),请确保已安装最新 SDK 后再编译运行.

## 快速开始

**解决 Git 不区分大小写**

```bash
git config core.ignorecase false
```

**构建与测试**

```bash
dotnet build
dotnet test -c Debug --no-build
```

脚本（可选）：

```bash
./Test.ps1
./Pack.ps1
```

示例项目：

```bash
dotnet watch run --project sample/WebApi.Test.Unit/WebApi.Test.Unit.csproj
```

<details>
<summary style="font-size: 14px">English</summary>

**Note:** This repo targets the latest .NET SDK (preview features)

```bash
git config core.ignorecase false
```

```bash
dotnet build
dotnet test -c Debug --no-build
```

Scripts (optional):

```bash
./Test.ps1
./Pack.ps1
```

Sample:

```bash
dotnet watch run --project sample/WebApi.Test.Unit/WebApi.Test.Unit.csproj
```

</details>

## 基础设施 (Docker)

- MongoDB 副本集：

```bash
docker compose -f docker-compose.mongo.rs.yml up -d
```

- 基础服务（测试用）：

```bash
docker compose -f docker-compose.basic.service.yml up -d
```

| 服务名称        | 服务描述 | 端口映射   | 镜像名称                                                 |
| --------------- | -------- | ---------- | -------------------------------------------------------- |
| Garnet          | 缓存     | 6379       | ghcr.io/microsoft/garnet:latest                          |
| RabbitMQ        | 消息队列 | 15672,5672 | rabbitmq:management                                      |
| AspireDashboard | 可观测性 | 18888,4317 | mcr.microsoft.com/dotnet/nightly/aspire-dashboard:latest |

<details>
<summary style="font-size: 14px">English</summary>

```bash
docker compose -f docker-compose.mongo.rs.yml up -d
docker compose -f docker-compose.basic.service.yml up -d
```

| Service         | Description   | Ports      | Image                                                    |
| --------------- | ------------- | ---------- | -------------------------------------------------------- |
| Garnet          | Cache         | 6379       | ghcr.io/microsoft/garnet:latest                          |
| RabbitMQ        | MQ            | 15672,5672 | rabbitmq:management                                      |
| AspireDashboard | Observability | 18888,4317 | mcr.microsoft.com/dotnet/nightly/aspire-dashboard:latest |

</details>

## 模块与文档索引

- Core
  - [EasilyNET.Core](./src/EasilyNET.Core/README.md)
  - [IO](./src/EasilyNET.Core/IO/README.md)
  - [Threading](./src/EasilyNET.Core/Threading/README.md)
  - [WebSocket Client](./src/EasilyNET.Core/WebSocket/README.md)
  - [Language](./src/EasilyNET.Core/Language/README.md)
  - [IDCard](./src/EasilyNET.Core/IDCard/README.md)
  - [Coordinate](./src/EasilyNET.Core/Coordinate/README.md)

- AutoDependencyInjection
  - [EasilyNET.AutoDependencyInjection](./src/EasilyNET.AutoDependencyInjection/README.md)
  - [EasilyNET.AutoDependencyInjection.Core](./src/EasilyNET.AutoDependencyInjection.Core/README.md)

- WebCore
  - [EasilyNET.WebCore](./src/EasilyNET.WebCore/README.md)
  - [WebSocket Server](./src/EasilyNET.WebCore/WebSocket/README.md)

- RabbitBus
  - [EasilyNET.RabbitBus.Core](./src/EasilyNET.RabbitBus.Core/README.md)
  - [EasilyNET.RabbitBus.AspNetCore](./src/EasilyNET.RabbitBus.AspNetCore/README.md)

- Mongo
  - [EasilyNET.Mongo.Core](./src/EasilyNET.Mongo.Core/README.md)
  - [EasilyNET.Mongo.AspNetCore](./src/EasilyNET.Mongo.AspNetCore/README.md)
  - [EasilyNET.Mongo.ConsoleDebug](./src/EasilyNET.Mongo.ConsoleDebug/README.md)

- Security
  - [EasilyNET.Security](./src/EasilyNET.Security/README.md)
  - [AES](./src/EasilyNET.Security/AES/README.md)
  - [RIPEMD](./src/EasilyNET.Security/RIPEMD/README.md)

- Sample
  - [WebApi.Test.Unit](./sample/WebApi.Test.Unit/README.md)

<details>
<summary style="font-size: 14px">English</summary>

Module docs are listed above. Some Mongo sub-packages currently have no standalone README yet.

</details>

#### 近期更新内容 (Recent Updates)

- 详细变更请查看各模块 README 的 Changelog/更新记录。

<details>
<summary style="font-size: 14px">English</summary>

- See module READMEs for changelogs and recent updates.

</details>

| NuGet Package                           | Version      | Download     | Document                                                  |
| --------------------------------------- | ------------ | ------------ | --------------------------------------------------------- |
| [EasilyNET.Core][8]                     | ![Nuget][9]  | ![Nuget][10] | [文档](./src/EasilyNET.Core/README.md)                    |
| [EasilyNET.WebCore][11]                 | ![Nuget][12] | ![Nuget][13] | [文档](./src/EasilyNET.WebCore/README.md)                 |
| [EasilyNET.AutoDependencyInjection][14] | ![Nuget][15] | ![Nuget][16] | [文档](./src/EasilyNET.AutoDependencyInjection/README.md) |
| [EasilyNET.RabbitBus.AspNetCore][17]    | ![Nuget][18] | ![Nuget][19] | [文档](./src/EasilyNET.RabbitBus.AspNetCore/README.md)    |
| [EasilyNET.Security][20]                | ![Nuget][21] | ![Nuget][22] | [文档](./src/EasilyNET.Security/README.md)                |
| [EasilyNET.Mongo.AspNetCore][23]        | ![Nuget][24] | ![Nuget][25] | [文档](./src/EasilyNET.Mongo.AspNetCore/README.md)        |
| [EasilyNET.Mongo.ConsoleDebug][26]      | ![Nuget][27] | ![Nuget][28] | [文档](./src/EasilyNET.Mongo.ConsoleDebug/README.md)      |

#### 感谢 [JetBrains](https://www.jetbrains.com/shop/eform/opensource) 对本项目的支持!

<img alt="Thanks JetBrains" src="https://www.jetbrains.com/shop/static/images/jetbrains-logo-inv.svg" height="100">

## 如何为本项目做出贡献

- Fork 本项目到你自己的仓库.
- 创建一个属于你自己的分支,名字随便你怎么取.
- 然后提交代码到你自己仓库的分支上.
- 然后到本项目创建一个 PR.
- 等待管理员合并 PR 后即可删除掉你自己的仓库.

<details>
<summary style="font-size: 14px">English</summary>

## How to contribute to this project

- Fork this repository to your own GitHub account.
- Create a new branch with any name you like.
- Commit your code to your branch.
- Create a Pull Request (PR) to this repository.
- After your PR is merged by the maintainer, you can delete your forked repository.

</details>

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
[14]: https://www.nuget.org/packages/EasilyNET.AutoDependencyInjection
[15]: https://img.shields.io/nuget/v/EasilyNET.AutoDependencyInjection
[16]: https://img.shields.io/nuget/dt/EasilyNET.AutoDependencyInjection
[17]: https://www.nuget.org/packages/EasilyNET.RabbitBus.AspNetCore
[18]: https://img.shields.io/nuget/v/EasilyNET.RabbitBus.AspNetCore
[19]: https://img.shields.io/nuget/dt/EasilyNET.RabbitBus.AspNetCore
[20]: https://www.nuget.org/packages/EasilyNET.Security
[21]: https://img.shields.io/nuget/v/EasilyNET.Security
[22]: https://img.shields.io/nuget/dt/EasilyNET.Security
[23]: https://www.nuget.org/packages/EasilyNET.Mongo.AspNetCore
[24]: https://img.shields.io/nuget/v/EasilyNET.Mongo.AspNetCore
[25]: https://img.shields.io/nuget/dt/EasilyNET.Mongo.AspNetCore
[26]: https://www.nuget.org/packages/EasilyNET.Mongo.ConsoleDebug
[27]: https://img.shields.io/nuget/v/EasilyNET.Mongo.ConsoleDebug
[28]: https://img.shields.io/nuget/dt/EasilyNET.Mongo.ConsoleDebug

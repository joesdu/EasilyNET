#### EasilyNET.Raft.AspNetCore

`EasilyNET.Raft.AspNetCore` 负责把 `Raft.Core + Storage.File + Transport.Grpc` 组装成可运行的 ASP.NET Core Raft 节点。一行注册、一行映射，即可启动一个具备选举、复制、快照、健康检查的 Raft 集群节点。

<details>
<summary style="font-size: 14px">English</summary>

`EasilyNET.Raft.AspNetCore` assembles `Raft.Core + Storage.File + Transport.Grpc` into a runnable ASP.NET Core Raft node. One line to register, one line to map — and you have a Raft cluster node with election, replication, snapshots, and health checks.

</details>

---

### 1. 快速上手（3 步启动集群）

#### Step 1: 安装 NuGet 包

```bash
dotnet add package EasilyNET.Raft.AspNetCore
```

#### Step 2: Program.cs 注册

```csharp
var builder = WebApplication.CreateBuilder(args);

// 注册 Raft 服务
builder.Services.AddEasilyRaft(
    raft =>
    {
        raft.NodeId = "n1";                              // 当前节点 ID
        raft.ClusterMembers = ["n1", "n2", "n3"];        // 集群成员（≥3，建议奇数）
        raft.ElectionTimeoutMinMs = 150;                 // 选举超时下限
        raft.ElectionTimeoutMaxMs = 300;                 // 选举超时上限
        raft.HeartbeatIntervalMs = 50;                   // 心跳间隔
        raft.EnablePreVote = true;                       // 启用 PreVote（推荐）
    },
    storage =>
    {
        storage.BaseDirectory = "./raft-data";           // 数据目录（确保可写）
        storage.FsyncPolicy = FsyncPolicy.Adaptive;     // 刷盘策略
    },
    grpc =>
    {
        grpc.PeerEndpoints = new Dictionary<string, string>
        {
            ["n1"] = "http://127.0.0.1:5001",
            ["n2"] = "http://127.0.0.1:5002",
            ["n3"] = "http://127.0.0.1:5003"
        };
    });

// 注册你的状态机（可选，默认为 NoopStateMachine）
// builder.Services.AddSingleton<IStateMachine, MyStateMachine>();

var app = builder.Build();

// 映射 Raft gRPC + 管理端点
app.MapEasilyRaft();
app.MapHealthChecks("/health");

app.Run();
```

#### Step 3: 启动 3 个节点

```bash
# 终端 1
dotnet run -- --urls http://127.0.0.1:5001

# 终端 2（修改 NodeId 为 n2）
dotnet run -- --urls http://127.0.0.1:5002

# 终端 3（修改 NodeId 为 n3）
dotnet run -- --urls http://127.0.0.1:5003
```

验证集群状态：

```bash
curl http://127.0.0.1:5001/raft/status
# 返回: {"nodeId":"n1","role":"Leader","currentTerm":1,...}
```

---

### 2. 模块职责

| 模块 | 职责 |
|------|------|
| `AddEasilyRaft()` | 一次注册所有核心依赖 |
| `RaftRuntime` | 串行执行动作、处理消息 |
| `RaftHostedService` | 驱动选举与心跳计时 |
| `RaftRpcMessageHandler` | 连接 gRPC 入站与运行时 |
| `MapEasilyRaft()` | 映射管理端点 + gRPC 服务 |
| `RaftMetrics` | 指标采集 |
| `RaftHealthCheck` | 健康检查探针 |

---

### 3. 管理端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/raft/status` | GET | 查询节点状态（role/term/commitIndex/leaderId） |
| `/raft/read-index` | GET | 线性一致读（Leader 确认 quorum 后返回安全读索引） |
| `/raft/members/add/{nodeId}` | POST | 添加集群成员 |
| `/raft/members/remove/{nodeId}` | POST | 移除集群成员 |

---

### 4. 实现原理

#### 4.1 运行时执行模型

- 内部通过 `SemaphoreSlim(1,1)` 串行处理所有消息，避免并发状态竞争。
- 动作执行分两阶段：
  - **Phase 1**：先执行所有持久化动作（Raft 要求 persist-before-respond）
  - **Phase 2**：再执行发送、应用、计时器动作
- 启动时自动恢复：从文件加载 state + WAL + snapshot。
- 恢复时**不重放未提交日志**，避免违反 State Machine Safety。

#### 4.2 后台心跳与选举驱动

- 选举循环：随机超时（`ElectionTimeoutMinMs` ~ `ElectionTimeoutMaxMs`）触发 `ElectionTimeoutElapsed`。
- 心跳循环：固定间隔（`HeartbeatIntervalMs`）触发 `HeartbeatTimeoutElapsed`。
- 计时器通过 `CancellationTokenSource` 交换实现无锁重置。

#### 4.3 指标与健康探针

指标（通过 `RaftMetrics`）：

- 选举次数、Leader 变更、Append 延迟、快照安装耗时
- commit 推进、term/role/commit/lastApplied/replicationLag

健康检查：

| 探针 | 说明 |
|------|------|
| `easilynet_raft` | 节点状态快照 |
| `easilynet_raft_liveness` | 事件循环是否正常 |
| `easilynet_raft_readiness` | 是否可服务（Leader 或可转发） |

---

### 5. 配置注意事项

#### 5.1 必须遵守的约束

| 约束 | 原因 |
|------|------|
| `NodeId` 必须在 `ClusterMembers` 中 | 否则节点无法参与选举 |
| `ElectionTimeoutMinMs > HeartbeatIntervalMs` | 否则心跳无法抑制选举 |
| `ClusterMembers.Count >= 3` 且为奇数 | 保证多数派可用 |
| 每个节点使用独立可写目录 | 避免数据冲突 |

#### 5.2 刷盘策略选择

| 策略 | 安全性 | 性能 | 适用场景 |
|------|--------|------|----------|
| `Always` | 最高 | 最低 | 金融、强一致场景 |
| `Batch` | 中等 | 较高 | 一般业务 |
| `Adaptive` | 自适应 | 自适应 | 推荐默认 |

> **注意**：无论选择哪种刷盘策略，`term/votedFor` 始终强制 fsync，确保 Raft 安全性。

#### 5.3 自定义状态机

默认注册 `NoopStateMachine`（不做任何事）。要实现业务逻辑，注册你自己的 `IStateMachine`：

```csharp
builder.Services.AddSingleton<IStateMachine, MyStateMachine>();
```

`IStateMachine` 需要实现：
- `ApplyAsync(entries)` — 将已提交日志应用到业务状态
- `CreateSnapshotAsync()` — 序列化当前状态为快照
- `RestoreSnapshotAsync(data)` — 从快照恢复状态

---

### 6. 故障排查

| 现象 | 排查步骤 |
|------|----------|
| Leader 频繁切换 | 检查网络 RTT 是否超过 `ElectionTimeoutMinMs`；调大超时 |
| 提交停滞 | 检查多数派节点是否可达；查看 `/raft/status` 的 commitIndex |
| 快照频繁触发 | 调高 `SnapshotThreshold`；优化状态机快照速度 |
| 启动失败 | 检查配置校验错误；确认数据目录权限 |
| 节点无法加入 | 确认 `PeerEndpoints` 包含新节点地址；先调用 `/raft/members/add` |

排查顺序：

1. `GET /raft/status` → 查看 role/term/commitIndex
2. 健康探针 → readiness/liveness
3. 指标 → leader 震荡与 replication lag
4. 日志 → 关键事件（选主、降级、快照安装）

#### EasilyNET.Raft.AspNetCore

`EasilyNET.Raft.AspNetCore` 负责把 `Raft.Core + Storage.File + Transport.Grpc` 组装成可运行的 ASP.NET Core Raft 节点。

---

### 1. 模块职责

- **DI 装配**：`AddEasilyRaft()` 一次注册核心依赖。
- **运行时驱动**：`RaftRuntime` 执行动作、串行处理消息。
- **后台循环**：`RaftHostedService` 驱动选举与心跳计时。
- **RPC 桥接**：`RaftRpcMessageHandler` 连接 gRPC 入站与运行时。
- **管理入口**：状态查询、ReadIndex、成员变更 API。
- **可观测性**：健康检查 + 指标采集。

---

### 2. 实现原理

#### 2.1 服务注册与配置校验

文件：`RaftServiceExtensions.cs`

- `services.AddEasilyRaft(...)` 注册：
  - `ILogStore` / `IStateStore` / `ISnapshotStore`（文件实现）
  - `IRaftTransport`（gRPC 实现）
  - `IRaftRuntime`（单线程运行时）
  - `RaftHostedService`
- `RaftOptionsValidator` + `ValidateOnStart()` 在启动即校验配置合法性。

#### 2.2 运行时执行模型

文件：`Runtime/RaftRuntime.cs`

- 内部通过 `_gate` 串行处理所有消息，避免并发状态竞争。
- 执行 `RaftNode` 输出的 `RaftAction`：
  - 持久化状态/日志
  - 应用状态机
  - 发送 RPC
  - 快照保存与日志截断
- 启动时自动恢复：state + WAL + snapshot。
- `ReadIndex` 走专门路径，附带 leader 侧确认逻辑。

#### 2.3 后台心跳与选举驱动

文件：`Services/RaftHostedService.cs`

- 选举循环：随机超时触发 `ElectionTimeoutElapsed`。
- 心跳循环：固定间隔触发 `HeartbeatTimeoutElapsed`。

#### 2.4 指标与健康探针

文件：`Observability/RaftMetrics.cs`、`Health/*`

- 指标：选举次数、Leader 变更、Append 延迟、快照安装耗时、commit 推进、term/role/commit/lastApplied/replicationLag。
- 健康检查：
  - `easilynet_raft`（状态快照）
  - `easilynet_raft_liveness`
  - `easilynet_raft_readiness`

#### 2.5 管理与运维端点

文件：`RaftEndpointRouteBuilderExtensions.cs`

- `GET /raft/status`
- `GET /raft/read-index`
- `POST /raft/members/add/{nodeId}`
- `POST /raft/members/remove/{nodeId}`

---

### 3. 使用方法

#### 3.1 Program.cs 注册

```csharp
builder.Services.AddEasilyRaft(
    raft =>
    {
        raft.NodeId = "n1";
        raft.ClusterMembers = ["n1", "n2", "n3"];
        raft.ElectionTimeoutMinMs = 150;
        raft.ElectionTimeoutMaxMs = 300;
        raft.HeartbeatIntervalMs = 50;
        raft.EnablePreVote = true;
    },
    storage =>
    {
        storage.BaseDirectory = "./raft-data";
        storage.FsyncPolicy = EasilyNET.Raft.Storage.File.Options.FsyncPolicy.Adaptive;
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
```

#### 3.2 映射端点

```csharp
app.MapEasilyRaft();
```

---

### 4. 最小运行建议

- 生产至少 3 节点，建议奇数节点规模。
- 确保 `NodeId` 在 `ClusterMembers` 中。
- `ElectionTimeoutMinMs > HeartbeatIntervalMs`。
- 使用独立可写目录存放 `raft-data`。

---

### 5. 故障排查入口

- 先看 `GET /raft/status` 中 role/term/commitIndex。
- 再看健康探针 readiness/liveness。
- 再结合指标观察 leader 震荡与 replication lag。

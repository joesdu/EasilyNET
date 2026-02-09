#### EasilyNET.Raft.Transport.Grpc

`EasilyNET.Raft.Transport.Grpc` 提供 Raft 的 gRPC 协议、消息映射、客户端传输与服务端 RPC 入口。

---

### 1. 设计目标

- **协议明确**：统一 `RequestVote` / `AppendEntries` / `InstallSnapshot`。
- **生产增强**：超时、重试、并发背压、快照流式分块。
- **无侵入接入**：对上游 Core 暴露 `IRaftTransport`。

---

### 2. 实现原理

#### 2.1 协议与映射

- `Protos/raft.proto`：定义 Raft RPC（含 `InstallSnapshotStream`）。
- `GrpcRaftMessageMapper.cs`：`RaftMessage <-> Protobuf` 双向转换。

#### 2.2 出站传输（客户端）

文件：`Transport/GrpcRaftTransport.cs`

- 维护每个 peer 的 `GrpcChannel` / `RaftRpcClient`。
- 使用 `SendWithRetryAsync` 处理瞬时错误重试。
- 使用 `RequestTimeoutMs` 控制超时。
- 通过 `MaxInFlightPerPeer` 信号量做每节点背压。
- `EnableAppendPipeline=true` 时，AppendEntries 可走 pipeline 发送。
- 快照超过 `SnapshotChunkBytes` 时自动走流式分块 RPC。

#### 2.3 入站服务（服务端）

文件：`Services/GrpcRaftService.cs`

- 把 RPC 请求映射为 `RaftMessage`。
- 调用 `IRaftRpcMessageHandler` 进入运行时。
- 再把返回消息映射回 gRPC 响应。

---

### 3. 配置项（RaftGrpcOptions）

文件：`Options/RaftGrpcOptions.cs`

- `PeerEndpoints`：`nodeId -> address`
- `RequestTimeoutMs`
- `MaxRetryAttempts`
- `RetryBackoffMs`
- `MaxInFlightPerPeer`
- `EnableAppendPipeline`
- `SnapshotChunkBytes`

---

### 4. 使用方法

#### 4.1 作为 `IRaftTransport` 注入（推荐）

```csharp
services.AddEasilyRaft(
    raft =>
    {
        raft.NodeId = "n1";
        raft.ClusterMembers = ["n1", "n2", "n3"];
    },
    configureGrpc: grpc =>
    {
        grpc.PeerEndpoints = new Dictionary<string, string>
        {
            ["n1"] = "http://127.0.0.1:5001",
            ["n2"] = "http://127.0.0.1:5002",
            ["n3"] = "http://127.0.0.1:5003"
        };
        grpc.RequestTimeoutMs = 1500;
        grpc.MaxRetryAttempts = 2;
    });
```

#### 4.2 映射 gRPC 服务端点

```csharp
app.MapEasilyRaft(); // 内含 MapGrpcService<GrpcRaftService>()
```

---

### 5. 适用场景

- 进程内/跨进程 Raft 节点通信
- 高延迟网络下需要超时重试与背压控制
- 快照体积较大，需要流式传输降低单次 RPC 压力

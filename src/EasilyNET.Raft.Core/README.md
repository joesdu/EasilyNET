#### EasilyNET.Raft.Core

`EasilyNET.Raft.Core` 是 Raft 的**纯状态机内核**：不做网络、不做磁盘、不启动线程，只负责把输入消息转换为状态变化与动作列表。

---

### 1. 设计目标

- **纯内核**：核心逻辑集中在 `RaftNode`，I/O 通过 `RaftAction` 外放。
- **可测试**：可直接喂消息并断言 `RaftResult`，无需真实集群环境。
- **可替换**：传输层、存储层、状态机都靠抽象接口接入。

核心契约：

- 输入：`RaftMessage + RaftNodeState`
- 输出：`RaftResult(State + Actions[])`

---

### 2. 核心实现原理

#### 2.1 消息驱动状态机

入口为 `Engine/RaftNode.cs` 的 `Handle(...)`，按消息类型分发：

- 选举超时 / 心跳超时
- `RequestVote` / `AppendEntries` / `InstallSnapshot`
- 客户端写请求 `ClientCommandRequest`
- 线性一致读 `ReadIndexRequest`
- 成员变更 `ConfigurationChangeRequest`

#### 2.2 选举与日志复制

- 支持 `PreVote`（由 `RaftOptions.EnablePreVote` 控制）。
- 候选者日志新旧比较遵循 Raft 规则（term 优先、index 次之）。
- Leader 追加日志后通过 `AppendEntries` 并根据响应推进 `matchIndex/nextIndex`。
- 提交推进遵循“**仅直接提交当前任期日志**”约束。

#### 2.3 ReadIndex 与成员变更

- `ReadIndexRequest` 由内核生成读响应动作，并触发心跳广播。
- 成员变更实现了两阶段过渡：
  - `cfg:joint:*`（过渡配置）
  - `cfg:final:*`（最终配置）
- 变更窗口禁止并发配置变更。

#### 2.4 Action 输出模型

`Actions/RaftActions.cs` 定义动作类型，常见包括：

- `SendMessageAction`
- `PersistStateAction`
- `PersistEntriesAction`
- `ApplyToStateMachineAction`
- `TakeSnapshotAction`
- `ResetElectionTimerAction` / `ResetHeartbeatTimerAction`

---

### 3. 主要类型与职责

- `Models/RaftNodeState.cs`：节点全量状态（角色、任期、日志、提交索引、复制游标、配置过渡状态）。
- `Messages/RaftMessage.cs` + `Messages/RaftRpcMessages.cs`：协议消息定义。
- `Abstractions/IRaftAbstractions.cs`：`IRaftTransport` / `ILogStore` / `IStateStore` / `ISnapshotStore` / `IStateMachine`。
- `Options/RaftOptions.cs`：算法参数配置。

---

### 4. 配置项（RaftOptions）

常用配置：

- `NodeId`
- `ClusterMembers`
- `ElectionTimeoutMinMs` / `ElectionTimeoutMaxMs`
- `HeartbeatIntervalMs`
- `MaxEntriesPerAppend`
- `SnapshotThreshold`
- `EnablePreVote`

---

### 5. 使用方法（内核直测示例）

```csharp
var options = new RaftOptions
{
    NodeId = "n1",
    ClusterMembers = ["n1", "n2", "n3"],
    EnablePreVote = true
};

var node = new RaftNode(options);
var state = new RaftNodeState
{
    NodeId = "n1",
    ClusterMembers = ["n1", "n2", "n3"]
};

var result = node.Handle(state, new ElectionTimeoutElapsed
{
    SourceNodeId = "n1",
    Term = state.CurrentTerm
});

// 执行 result.Actions 由外层负责
```

---

### 6. 适用场景

- 你要做自定义传输（非 gRPC）
- 你要做自定义存储（非文件）
- 你要做高可控的仿真/验证测试

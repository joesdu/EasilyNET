#### EasilyNET.Raft.Core

`EasilyNET.Raft.Core` 是 Raft 的**纯状态机内核**：不做网络、不做磁盘、不启动线程，只负责把输入消息转换为状态变化与动作列表。

<details>
<summary style="font-size: 14px">English</summary>

`EasilyNET.Raft.Core` is a **pure state machine kernel** for Raft: no networking, no disk I/O, no threads — it only converts input messages into state changes and action lists.

</details>

---

### 1. 设计目标

- **纯内核**：核心逻辑集中在 `RaftNode`，I/O 通过 `RaftAction` 外放。
- **可测试**：可直接喂消息并断言 `RaftResult`，无需真实集群环境。
- **可替换**：传输层、存储层、状态机都靠抽象接口接入。

核心契约：

```
输入：RaftMessage + RaftNodeState
输出：RaftResult(State + Actions[])
```

---

### 2. 安全性保证

本实现遵循 Raft 论文的五大安全性保证：

| 保证 | 含义 | 实现方式 |
|------|------|----------|
| Election Safety | 每个 term 最多一个 Leader | 每 term 只投一票 + PreVote 防止 term 膨胀 |
| Leader Append-Only | Leader 仅追加，不覆盖自身日志 | `HandleClientCommand` 只做 `Log.Add` |
| Log Matching | 同 index+term 的日志，之前前缀必一致 | `MatchPrevLog` 一致性检查 + 冲突快速回退 |
| Leader Completeness | 已提交日志必出现在后续 Leader 中 | `IsCandidateUpToDate` 投票限制 |
| State Machine Safety | 同 index 不会应用不同命令 | 仅提交后才应用 + no-op 间接提交前任期日志 |

关键实现细节：

- **PreVote 不触发 StepDown**：PreVote 请求不会导致接收方 term 膨胀，避免网络恢复后的选举风暴。
- **Leader no-op**：新 Leader 上任后立即追加一条当前任期的空日志，确保前任期未提交日志可被间接提交（Raft §5.4.2）。
- **仅提交当前任期日志**：`TryAdvanceCommit` 只直接提交 `entry.Term == currentTerm` 的日志。
- **日志冲突快速回退**：`AppendEntriesResponse` 携带 `ConflictTerm/ConflictIndex`，Leader 可跳过整个冲突 term。

---

### 3. 核心实现原理

#### 3.1 消息驱动状态机

入口为 `Engine/RaftNode.cs` 的 `Handle(...)`，按消息类型分发：

- 选举超时 / 心跳超时
- `RequestVote` / `AppendEntries` / `InstallSnapshot`
- 客户端写请求 `ClientCommandRequest`
- 线性一致读 `ReadIndexRequest`
- 成员变更 `ConfigurationChangeRequest`

#### 3.2 选举与日志复制

- 支持 `PreVote`（由 `RaftOptions.EnablePreVote` 控制），防止网络分区恢复后的 term 膨胀。
- 候选者日志新旧比较遵循 Raft 规则（term 优先、index 次之）。
- Leader 追加日志后通过 `AppendEntries` 并根据响应推进 `matchIndex/nextIndex`。
- 提交推进遵循"**仅直接提交当前任期日志**"约束。
- 日志查找使用 **O(log n) 二分查找**，适合大规模日志。

#### 3.3 ReadIndex 与成员变更

- `ReadIndexRequest` 由内核生成读响应动作，并触发心跳广播确认 Leader 身份。
- 成员变更实现了两阶段联合共识过渡：
  - `cfg:joint:*`（联合配置 — 新旧配置同时生效）
  - `cfg:final:*`（最终配置 — 仅新配置生效）
- 变更窗口禁止并发配置变更，一次只增/删一个节点。

#### 3.4 Action 输出模型

`Actions/RaftActions.cs` 定义动作类型：

| Action | 职责 |
|--------|------|
| `SendMessageAction` | 发送 RPC 到目标节点 |
| `PersistStateAction` | 持久化 term/votedFor（**必须在响应前完成**） |
| `PersistEntriesAction` | 持久化日志条目 |
| `TruncateLogSuffixAction` | 截断冲突日志后缀 |
| `ApplyToStateMachineAction` | 应用已提交日志到状态机 |
| `TakeSnapshotAction` | 安装远端快照 |
| `ResetElectionTimerAction` | 重置选举计时器 |
| `ResetHeartbeatTimerAction` | 重置心跳计时器 |
| `SendSnapshotToPeerAction` | 向落后 Follower 发送快照 |

---

### 4. 主要类型与职责

| 类型 | 职责 |
|------|------|
| `RaftNode` | 纯状态机引擎，处理消息并输出动作 |
| `RaftNodeState` | 节点全量状态（角色、任期、日志、提交索引、复制游标、配置过渡） |
| `RaftMessage` / `RaftRpcMessages` | 协议消息定义 |
| `IRaftTransport` | 传输抽象（发送 RPC） |
| `ILogStore` | 日志存储抽象（追加、截断、压缩） |
| `IStateStore` | 状态存储抽象（term/votedFor） |
| `ISnapshotStore` | 快照存储抽象 |
| `IStateMachine` | 用户状态机抽象（应用日志、创建/恢复快照） |
| `RaftOptions` | 算法参数配置 |

---

### 5. 配置项（RaftOptions）

| 配置 | 默认值 | 说明 |
|------|--------|------|
| `NodeId` | `""` | 当前节点 ID（必填） |
| `ClusterMembers` | `[]` | 集群成员列表（≥3，建议奇数） |
| `ElectionTimeoutMinMs` | `150` | 最小选举超时（必须 > HeartbeatIntervalMs） |
| `ElectionTimeoutMaxMs` | `300` | 最大选举超时（必须 > ElectionTimeoutMinMs） |
| `HeartbeatIntervalMs` | `50` | 心跳间隔 |
| `MaxEntriesPerAppend` | `100` | 单次 AppendEntries 最大条目数 |
| `SnapshotThreshold` | `10000` | 快照触发阈值（日志条目数） |
| `EnablePreVote` | `true` | 启用 PreVote（生产建议开启） |

---

### 6. 使用方法

#### 6.1 内核直测示例

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

// 模拟选举超时
var result = node.Handle(state, new ElectionTimeoutElapsed
{
    SourceNodeId = "n1",
    Term = state.CurrentTerm
});

// result.Actions 包含需要执行的副作用（发送 RPC、持久化等）
// 由外层运行时负责执行
foreach (var action in result.Actions)
{
    // 按类型分发执行...
}
```

#### 6.2 自定义状态机

```csharp
public class MyStateMachine : IStateMachine
{
    private readonly Dictionary<string, string> _store = new();

    public Task ApplyAsync(IReadOnlyList<RaftLogEntry> entries, CancellationToken ct = default)
    {
        foreach (var entry in entries)
        {
            var command = Encoding.UTF8.GetString(entry.Command);
            // ... 解析并应用命令到业务状态
        }
        return Task.CompletedTask;
    }

    public Task<byte[]> CreateSnapshotAsync(CancellationToken ct = default)
        => Task.FromResult(JsonSerializer.SerializeToUtf8Bytes(_store));

    public Task RestoreSnapshotAsync(byte[] data, CancellationToken ct = default)
    {
        var restored = JsonSerializer.Deserialize<Dictionary<string, string>>(data);
        _store.Clear();
        foreach (var kv in restored ?? []) _store[kv.Key] = kv.Value;
        return Task.CompletedTask;
    }
}
```

---

### 7. 适用场景

- 你要做自定义传输（非 gRPC）→ 实现 `IRaftTransport`
- 你要做自定义存储（非文件）→ 实现 `ILogStore` / `IStateStore` / `ISnapshotStore`
- 你要做高可控的仿真/验证测试 → 直接调用 `RaftNode.Handle()` 并断言结果
- 你要嵌入到已有框架中 → Core 无任何外部依赖，可独立使用

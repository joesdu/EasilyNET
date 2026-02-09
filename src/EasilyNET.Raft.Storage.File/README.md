#### EasilyNET.Raft.Storage.File

`EasilyNET.Raft.Storage.File` 提供 Raft 的文件持久化实现，覆盖状态、WAL、快照三条存储路径。

---

### 1. 设计目标

- **崩溃恢复可用**：重启后可恢复 term/vote、日志与快照。
- **可配置刷盘策略**：在一致性与性能间可按场景权衡。
- **实现简单透明**：文件格式清晰，便于排障与运维检查。

---

### 2. 实现原理

#### 2.1 `FileStateStore`

文件：`Stores/FileStateStore.cs`

- 保存 `(currentTerm, votedFor)`。
- 写入流程采用 **temp 文件 + move 覆盖**，避免半写状态。
- 结合 `FlushPolicyDecider` 决定是否 `Flush(flushToDisk: true)`。

#### 2.2 `FileLogStore`

文件：`Stores/FileLogStore.cs`

- WAL 使用**每行一条 JSON**（`index/term/command`）。
- `AppendAsync` 追加写入并按策略刷盘。
- `TruncateSuffixAsync` 通过重写临时文件实现安全截断。

#### 2.3 `FileSnapshotStore`

文件：`Stores/FileSnapshotStore.cs`

- 元数据与数据分离：`snapshot.meta.json` + `snapshot.bin`。
- 保存时先写临时文件，再原子替换。
- 读取时要求元数据和数据文件都存在。

#### 2.4 刷盘策略决策

文件：`Stores/FlushPolicyDecider.cs`

- `Always`：每次写后刷盘。
- `Batch`：按 `BatchFlushIntervalMs` 时间窗口刷盘。
- `Adaptive`：低写压时近似 Always，高写压退化为 Batch。

---

### 3. 配置项（RaftFileStorageOptions）

文件：`Options/RaftFileStorageOptions.cs`

- `BaseDirectory`
- `StateFileName`
- `LogFileName`
- `SnapshotMetadataFileName`
- `SnapshotDataFileName`
- `FsyncPolicy`
- `BatchFlushIntervalMs`
- `AdaptiveHighLoadWritesPerSecond`

刷盘策略枚举见：`Options/FsyncPolicy.cs`

---

### 4. 使用方法

#### 4.1 直接创建存储实例

```csharp
var options = new RaftFileStorageOptions
{
    BaseDirectory = Path.Combine(AppContext.BaseDirectory, "raft-data"),
    FsyncPolicy = FsyncPolicy.Adaptive,
    BatchFlushIntervalMs = 100
};

var stateStore = new FileStateStore(options);
var logStore = new FileLogStore(options);
var snapshotStore = new FileSnapshotStore(options);
```

#### 4.2 在宿主中通过 `AddEasilyRaft` 注入

```csharp
services.AddEasilyRaft(
    raft =>
    {
        raft.NodeId = "n1";
        raft.ClusterMembers = ["n1", "n2", "n3"];
    },
    storage =>
    {
        storage.BaseDirectory = "./raft-data";
        storage.FsyncPolicy = FsyncPolicy.Batch;
    });
```

---

### 5. 运维建议

- 生产环境优先将 `BaseDirectory` 放到可靠磁盘。
- 对写吞吐敏感场景可从 `Always` 调整为 `Batch/Adaptive`。
- 定期巡检 `wal.log` 与快照文件大小，结合 `SnapshotThreshold` 调优。

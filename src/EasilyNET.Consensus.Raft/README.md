# EasilyNET.Consensus.Raft

一个基于 .NET 的轻量级 Raft 共识算法实现，使用 StreamJsonRpc 作为传输层。适合入门学习、演示与原型开发。

状态与范围
- 实现：领导者选举、心跳、日志复制（基础 happy-path）。
- 内存实现：日志与状态均在内存中，不做持久化。
- 暂未实现：磁盘持久化、快照/日志压缩、动态成员变更等。

安装

```
dotnet add package EasilyNET.Consensus.Raft
```

快速上手（最短路径）
1) 定义节点地址并创建集群：

```csharp
using EasilyNET.Consensus.Raft;

var serviceDiscovery = new StaticServiceDiscovery(new Dictionary<string, NodeAddress>
{
    ["node1"] = new("localhost", 5001),
    ["node2"] = new("localhost", 5002),
    ["node3"] = new("localhost", 5003),
});

var nodeIds = new List<string> { "node1", "node2", "node3" };
var cluster = new StreamJsonRpcRaftCluster(nodeIds, serviceDiscovery);
await cluster.StartAsync();
```

2) 提交命令到领导者：

```csharp
var leader = cluster.GetLeader();
if (leader != null)
{
    var cmd = System.Text.Encoding.UTF8.GetBytes("SET key value");
    var ok = await leader.AppendLog(cmd);
    Console.WriteLine($"append result = {ok}");
}
```

3) 结束时停止：

```csharp
await cluster.StopAsync();
```

核心概念（小白友好）
- 节点（RaftNode）：一个参与者，持有日志、任期与状态（Follower/Candidate/Leader）。
  - 公开属性：NodeId, State, CurrentTerm。
  - 重要方法：Start/Stop, AppendLog(byte[] command)。
- 传输（IRaftRpc）：节点间通信接口。
  - 方法：RequestVoteAsync、AppendEntriesAsync。
- 实现（StreamJsonRpcRaftRpc）：基于 TCP + StreamJsonRpc 的传输实现。
  - 每个节点仅启动自己的 TCP 监听；需要与其他节点通信时按需建立客户端连接。
- 集群（StreamJsonRpcRaftCluster）：帮助一次性创建并启动多节点（用于本机演示或测试）。
- 服务发现（IServiceDiscovery）：从 NodeId 映射到 NodeAddress（Host/Port）。
  - 提供 StaticServiceDiscovery 与 DnsServiceDiscovery 两种实现。

工作原理（一句话版）
- Follower 在一段随机时间未收到心跳后，会自增任期并发起投票（Candidate）；
- 获得多数票后成为 Leader；
- Leader 周期性发送心跳（AppendEntries 空 entries），并在收到客户端命令时将日志复制到多数节点后提交。

可调参数
- RaftConfig.ElectionTimeoutMs：选举超时（默认 150ms 基础 + 抖动）。
- RaftConfig.HeartbeatIntervalMs：心跳间隔（默认 50ms）。

服务发现用法
- 静态配置：
```csharp
var sd = new StaticServiceDiscovery(new Dictionary<string, NodeAddress>
{
    ["node1"] = new("10.0.0.1", 5001),
    ["node2"] = new("10.0.0.2", 5002),
});
```
- 基于 DNS：
```csharp
var sd = new DnsServiceDiscovery(".cluster.local", defaultPort: 5001);
// 节点会解析形如 node1.cluster.local 的地址
```

事件
- RaftNode.OnEntryApplied：当条目提交并应用到状态机时触发。

```csharp
node.OnEntryApplied += (sender, entry) =>
{
    Console.WriteLine($"applied: {System.Text.Encoding.UTF8.GetString(entry.Command!)}");
};
```

生产落地建议
- 为 CurrentTerm、VotedFor、Log 增加持久化与恢复；实现快照/压缩。
- 加入网络超时、重试、指数退避，完善失败恢复与分区处理。
- 传输层开启 TLS/鉴权；完善监控、指标与告警。

常见问题
- 节点如何互相发现？实现或选择一个 IServiceDiscovery（本包提供静态/基于 DNS 的实现）。

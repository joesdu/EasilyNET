# EasilyNET.Consensus.Raft

[![NuGet](https://img.shields.io/nuget/v/EasilyNET.Consensus.Raft.svg)](https://www.nuget.org/packages/EasilyNET.Consensus.Raft/)
[![License](https://img.shields.io/github/license/joesdu/EasilyNET)](https://github.com/joesdu/EasilyNET/blob/dev/LICENSE)

此包为 .NET 提供了完整的 Raft 共识算法实现，用于构建分布式系统，确保强一致性。

## 功能特性

- **领导者选举**：自动选举领导者节点
- **日志复制**：确保所有节点的数据一致性
- **安全性保证**：实现 Raft 算法的所有安全性特性
- **故障容错**：支持节点故障和网络分区
- **多通信协议**：支持 WebSocket、StreamJsonRpc TCP 通信
- **异步通信**：完全异步的消息处理
- **内存 RPC**：内置内存 RPC 实现用于测试
- **服务发现**：支持多种服务发现机制（静态配置、DNS、etcd 等）
- **高性能**：StreamJsonRpc 提供低延迟、高吞吐量的通信

## 安装

```bash
dotnet add package EasilyNET.Consensus.Raft
```

## 基本概念

### 节点状态

- **Follower**：跟随者状态，响应领导者的请求
- **Candidate**：候选者状态，参与领导者选举
- **Leader**：领导者状态，负责处理客户端请求和日志复制

### 核心组件

- `RaftNode`：单个 Raft 节点实现
- `WebSocketRaftCluster`：基于 WebSocket 的集群管理器
- `StreamJsonRpcRaftCluster`：基于 StreamJsonRpc TCP 的集群管理器
- `WebSocketRaftRpc`：WebSocket RPC 实现
- `StreamJsonRpcRaftRpc`：StreamJsonRpc TCP RPC 实现
- `WebSocketRaftServer`：WebSocket 服务器
- `StreamJsonRpcRaftServer`：StreamJsonRpc TCP 服务器
- `IRaftRpc`：RPC 通信接口
- `RaftConfig`：配置类
- `IServiceDiscovery`：服务发现接口
- `ServiceDiscoveryFactory`：服务发现工厂

## 使用方法

### 1. 使用 StreamJsonRpc TCP 通信（推荐）

StreamJsonRpc 提供更可靠、更高效的 TCP 通信，适合生产环境：

```csharp
using EasilyNET.Consensus.Raft;

// 创建服务发现实例
var serviceDiscovery = new StaticServiceDiscovery(new Dictionary<string, (string Host, int Port)>
{
    ["node1"] = ("localhost", 5001),
    ["node2"] = ("localhost", 5002),
    ["node3"] = ("localhost", 5003)
});

// 创建节点ID列表
var nodeIds = new List<string> { "node1", "node2", "node3" };

// 创建 StreamJsonRpc 集群
using var cluster = new StreamJsonRpcRaftCluster(nodeIds, serviceDiscovery);

// 启动集群
await cluster.StartAsync();

// 获取领导者
var leader = cluster.GetLeader();
Console.WriteLine($"当前领导者: {leader?.NodeId}");

// 提交日志条目
var logEntry = new LogEntry
{
    Term = leader.CurrentTerm,
    Command = "SET key1 value1",
    Index = leader.LastLogIndex + 1
};

var success = await leader.ReplicateLogEntryAsync(logEntry);
Console.WriteLine($"日志复制: {(success ? "成功" : "失败")}");

// 停止集群
await cluster.StopAsync();
```

### 2. 使用 WebSocket 通信

```csharp
// 创建节点地址映射
var nodeAddresses = new Dictionary<string, NodeAddress>
{
    ["node1"] = new NodeAddress("192.168.1.100", 8081),
    ["node2"] = new NodeAddress("192.168.1.101", 8082),
    ["node3"] = new NodeAddress("192.168.1.102", 8083)
};

// 创建静态服务发现实例
var serviceDiscovery = ServiceDiscoveryFactory.CreateStatic(nodeAddresses);

// 创建节点ID列表
var nodeIds = new List<string> { "node1", "node2", "node3" };

// 创建WebSocket集群
using var cluster = new WebSocketRaftCluster(nodeIds, serviceDiscovery);

// 启动集群
await cluster.StartAsync();

// 获取领导者
var leader = cluster.GetLeader();

// 追加日志
var command = Encoding.UTF8.GetBytes("Hello World");
await leader.AppendLog(command);

// 停止集群
await cluster.StopAsync();
```

### 2. DNS-based 服务发现

```csharp
// 创建DNS-based服务发现（节点域名格式：node1.cluster.local）
var serviceDiscovery = ServiceDiscoveryFactory.CreateDnsBased(".cluster.local", 8080);

// 创建集群
var nodeIds = new List<string> { "node1", "node2", "node3" };
using var cluster = new WebSocketRaftCluster(nodeIds, serviceDiscovery);

// 启动集群
await cluster.StartAsync();
```

### 3. 配置文件-based 服务发现

```csharp
// 从配置文件创建服务发现
var serviceDiscovery = await ServiceDiscoveryFactory.CreateFromConfigFileAsync("cluster-config.json");

// cluster-config.json 内容：
// {
//   "node1": { "host": "192.168.1.100", "port": 8081 },
//   "node2": { "host": "192.168.1.101", "port": 8082 },
//   "node3": { "host": "192.168.1.102", "port": 8083 }
// }
```

### 4. 传统端口配置方式（向后兼容）

```csharp
// 创建节点配置 (节点ID, 端口)
var nodeConfigs = new List<(string NodeId, int Port)>
{
    ("node1", 8081),
    ("node2", 8082),
    ("node3", 8083)
};

// 创建WebSocket集群
using var cluster = new WebSocketRaftCluster(nodeConfigs);

// 启动集群
await cluster.StartAsync();
```

### 2. 单个节点使用

```csharp
// 创建节点配置
var config = new RaftConfig("node1", new List<string> { "node1", "node2", "node3" });

// 创建WebSocket RPC
var nodePorts = new Dictionary<string, int>
{
    ["node1"] = 8081,
    ["node2"] = 8082,
    ["node3"] = 8083
};
var rpc = new WebSocketRaftRpc("node1", nodePorts);

// 创建节点
var node = new RaftNode(config, rpc);

// 添加其他节点
rpc.AddNode("node2", 8082, node);
rpc.AddNode("node3", 8083, node);

// 启动
await rpc.StartAsync();
node.Start();

// 使用
await node.AppendLog(Encoding.UTF8.GetBytes("test"));

// 停止
node.Stop();
await rpc.StopAsync();
```

### 3. 传统端口配置方式（向后兼容）

```csharp
// 创建节点配置 (节点ID, 端口)
var nodeConfigs = new List<(string NodeId, int Port)>
{
    ("node1", 8081),
    ("node2", 8082),
    ("node3", 8083)
};

// 创建WebSocket集群
using var cluster = new WebSocketRaftCluster(nodeConfigs);

// 启动集群
await cluster.StartAsync();
```

```csharp
var config = new RaftConfig("node1", clusterNodes)
{
    ElectionTimeoutMs = 200,  // 选举超时时间
    HeartbeatIntervalMs = 50   // 心跳间隔
};
```

## 服务发现机制

### 概述

服务发现机制允许 Raft 集群在分布式环境中动态发现和连接节点，而不必硬编码 IP 地址和端口。这对于云环境、容器化部署和动态扩缩容非常重要。

### 支持的服务发现类型

#### 1. 静态配置服务发现

适用于开发环境和小型固定集群：

```csharp
var nodeAddresses = new Dictionary<string, NodeAddress>
{
    ["node1"] = new NodeAddress("192.168.1.100", 8081),
    ["node2"] = new NodeAddress("192.168.1.101", 8082),
    ["node3"] = new NodeAddress("192.168.1.102", 8083)
};

var serviceDiscovery = ServiceDiscoveryFactory.CreateStatic(nodeAddresses);
```

#### 2. DNS-based 服务发现

适用于 Kubernetes 和有 DNS 服务的环境：

```csharp
// 节点域名格式：node1.cluster.local, node2.cluster.local, ...
var serviceDiscovery = ServiceDiscoveryFactory.CreateDnsBased(".cluster.local", 8080);
```

#### 3. 配置文件服务发现

适用于配置驱动的环境：

```csharp
var serviceDiscovery = await ServiceDiscoveryFactory.CreateFromConfigFileAsync("cluster.json");
```

#### 4. 自定义服务发现

实现 `IServiceDiscovery` 接口：

```csharp
public class CustomServiceDiscovery : IServiceDiscovery
{
    public async Task<NodeAddress?> GetNodeAddressAsync(string nodeId)
    {
        // 自定义发现逻辑
        return await DiscoverNodeAddress(nodeId);
    }

    // 实现其他接口方法...
}
```

### 服务发现接口

```csharp
public interface IServiceDiscovery
{
    Task<NodeAddress?> GetNodeAddressAsync(string nodeId);
    Task<Dictionary<string, NodeAddress>> GetAllNodeAddressesAsync();
    Task RegisterNodeAsync(string nodeId, NodeAddress address);
    Task UnregisterNodeAsync(string nodeId);
    Task RefreshAsync();
}
```

### 部署建议

#### 生产环境部署

1. **使用 DNS 服务发现**：

   ```csharp
   var serviceDiscovery = ServiceDiscoveryFactory.CreateDnsBased(".raft-cluster.svc.cluster.local", 8080);
   ```

2. **配置外部服务发现**：

   - etcd
   - Consul
   - ZooKeeper
   - Kubernetes Service

3. **负载均衡器配置**：
   - 配置健康检查端点
   - 设置适当的超时时间
   - 启用会话保持

#### 网络配置

1. **防火墙规则**：

   ```bash
   # 开放 Raft 通信端口
   firewall-cmd --add-port=8080-8090/tcp --permanent
   ```

2. **网络安全**：
   - 使用 TLS 加密 WebSocket 连接
   - 实施网络分段
   - 配置访问控制列表

## 网络通信

### WebSocket 服务器

每个节点运行一个 WebSocket 服务器，监听指定端口：

```
ws://localhost:8081/raft/
ws://localhost:8082/raft/
ws://localhost:8083/raft/
```

### 消息格式

所有通信使用 JSON 格式的消息：

```json
{
  "messageType": "RequestVote",
  "targetNodeId": "node2",
  "payload": "{...}",
  "messageId": "uuid"
}
```

### 连接管理

- **自动重连**：客户端断开后自动重连
- **心跳检测**：通过定期心跳检测连接状态
- **超时处理**：请求超时自动取消
- **错误处理**：网络异常自动重试

## 安全性保证

实现以下 Raft 安全性特性：

1. **选举安全性**：一个任期内最多只有一个领导者
2. **领导者只附加**：领导者从不覆盖或删除自己的日志条目
3. **日志匹配**：相同索引和任期的日志条目完全相同
4. **领导者完整性**：已提交的条目出现在所有后续领导者中
5. **状态机安全性**：相同索引的条目只应用一次

## 事件处理

```csharp
var node = new RaftNode(config, rpc);
node.OnEntryApplied += (sender, entry) =>
{
    // 处理已应用的日志条目
    Console.WriteLine($"Applied entry: {Encoding.UTF8.GetString(entry.Command)}");
};
```

## Raft 共识算法使用场景

### 适用场景

Raft 共识算法适用于需要强一致性保证的分布式系统：

#### 1. 分布式数据库

- **数据复制**：确保多节点数据一致性
- **事务协调**：分布式事务的协调和提交
- **故障恢复**：自动故障检测和数据恢复

```csharp
// 分布式键值存储示例
public class DistributedKeyValueStore
{
    private readonly RaftNode _raftNode;

    public async Task SetAsync(string key, string value)
    {
        var command = $"SET {key} {value}";
        var logEntry = new LogEntry
        {
            Term = _raftNode.CurrentTerm,
            Command = command,
            Index = _raftNode.LastLogIndex + 1
        };

        await _raftNode.ReplicateLogEntryAsync(logEntry);
    }

    public string Get(string key)
    {
        // 从本地状态机读取数据
        return _localStore.Get(key);
    }
}
```

#### 2. 分布式锁服务

- **互斥锁**：分布式环境下的资源锁定
- **领导者选举**：服务实例间的协调
- **配置管理**：分布式配置的同步更新

```csharp
// 分布式锁示例
public class DistributedLock
{
    private readonly RaftNode _raftNode;

    public async Task<bool> AcquireLockAsync(string lockKey, string ownerId)
    {
        var command = $"LOCK {lockKey} {ownerId}";
        var logEntry = new LogEntry
        {
            Term = _raftNode.CurrentTerm,
            Command = command,
            Index = _raftNode.LastLogIndex + 1
        };

        return await _raftNode.ReplicateLogEntryAsync(logEntry);
    }

    public async Task ReleaseLockAsync(string lockKey, string ownerId)
    {
        var command = $"UNLOCK {lockKey} {ownerId}";
        var logEntry = new LogEntry
        {
            Term = _raftNode.CurrentTerm,
            Command = command,
            Index = _raftNode.LastLogIndex + 1
        };

        await _raftNode.ReplicateLogEntryAsync(logEntry);
    }
}
```

#### 3. 分布式配置中心

- **配置同步**：跨服务实例的配置更新
- **版本控制**：配置变更的历史记录
- **回滚支持**：配置错误的快速回滚

```csharp
// 配置中心示例
public class ConfigurationCenter
{
    private readonly RaftNode _raftNode;

    public async Task UpdateConfigurationAsync(string key, string value)
    {
        var command = $"CONFIG {key} {value}";
        var logEntry = new LogEntry
        {
            Term = _raftNode.CurrentTerm,
            Command = command,
            Index = _raftNode.LastLogIndex + 1
        };

        await _raftNode.ReplicateLogEntryAsync(logEntry);
    }

    public async Task RollbackConfigurationAsync(int targetVersion)
    {
        var command = $"ROLLBACK {targetVersion}";
        var logEntry = new LogEntry
        {
            Term = _raftNode.CurrentTerm,
            Command = command,
            Index = _raftNode.LastLogIndex + 1
        };

        await _raftNode.ReplicateLogEntryAsync(logEntry);
    }
}
```

#### 4. 分布式消息队列

- **消息去重**：确保消息只被处理一次
- **顺序保证**：维护消息的处理顺序
- **故障转移**：消息处理的自动故障转移

#### 5. 分布式协调服务

- **服务注册**：微服务实例的注册和发现
- **健康检查**：服务实例的健康状态监控
- **负载均衡**：基于一致性哈希的负载均衡

### 性能优化建议

#### 1. 网络优化

```csharp
// 使用 StreamJsonRpc 获得更好的性能
var cluster = new StreamJsonRpcRaftCluster(nodeIds, serviceDiscovery);
```

#### 2. 配置调优

```csharp
var config = new RaftConfig(nodeId, nodeIds)
{
    ElectionTimeoutMs = 300,      // 选举超时时间
    HeartbeatIntervalMs = 100,    // 心跳间隔
    MaxBatchSize = 100,           // 批量处理大小
    SnapshotInterval = 1000       // 快照间隔
};
```

#### 3. 监控和告警

```csharp
node.OnStateChanged += (sender, state) =>
{
    Console.WriteLine($"节点状态变更: {state}");
    // 发送告警通知
};

node.OnLeaderElected += (sender, leaderId) =>
{
    Console.WriteLine($"新领导者选举: {leaderId}");
    // 记录领导者变更事件
};
```

### 部署架构

#### 单数据中心部署

```
[Client] --> [Load Balancer] --> [Raft Node 1] (Leader)
                              --> [Raft Node 2] (Follower)
                              --> [Raft Node 3] (Follower)
```

#### 多数据中心部署

```
Data Center 1           Data Center 2           Data Center 3
[Node 1] (Leader) <--> [Node 4] (Follower) <--> [Node 7] (Follower)
[Node 2] (Follower) --> [Node 5] (Follower) --> [Node 8] (Follower)
[Node 3] (Follower) --> [Node 6] (Follower) --> [Node 9] (Follower)
```

### 容量规划

#### 节点数量建议

- **最小配置**：3 个节点（容忍 1 个节点故障）
- **中等规模**：5 个节点（容忍 2 个节点故障）
- **大规模**：7 个节点（容忍 3 个节点故障）

#### 硬件配置建议

- **CPU**：4 核以上，推荐 8 核
- **内存**：8GB 以上，推荐 16GB
- **存储**：SSD 存储，推荐 NVMe
- **网络**：万兆网络，延迟 < 1ms

## 依赖注入集成

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// 添加 Raft 服务
services.AddRaftConsensus("node1", new List<string> { "node1", "node2", "node3" });

var serviceProvider = services.BuildServiceProvider();

// 启动节点
await serviceProvider.StartRaftNodeAsync();

// 使用节点
var node = serviceProvider.GetRequiredService<RaftNode>();
await node.AppendLog(Encoding.UTF8.GetBytes("test"));

// 停止节点
serviceProvider.StopRaftNode();
```

## 演示程序

### StreamJsonRpc TCP 演示

运行 StreamJsonRpc 演示程序：

```csharp
// 运行 StreamJsonRpc Raft 演示
await StreamJsonRpcRaftDemo.Main(args);
```

演示程序将：

1. 创建 3 个节点的 StreamJsonRpc TCP 集群
2. 启动所有节点并等待选举完成
3. 提交测试命令并验证日志复制
4. 显示集群状态和领导者信息
5. 模拟网络分区和恢复场景

### WebSocket 演示

运行传统 WebSocket 演示程序：

```csharp
// 运行 RaftDemo 程序
await RaftDemo.Program.Main(args);
```

演示程序将：

1. 创建 3 个节点的 WebSocket 集群
2. 启动所有节点并等待选举
3. 追加日志条目并验证复制
4. 模拟领导者故障和恢复
5. 显示集群状态

## 注意事项

1. **端口配置**：确保各节点的端口不冲突
2. **防火墙**：开放 WebSocket 端口访问
3. **网络延迟**：根据网络条件调整超时时间
4. **节点数量**：建议至少 3 个节点以实现容错
5. **资源管理**：正确释放 WebSocket 连接资源
6. **服务发现**：选择适合部署环境的服务发现机制
7. **网络分区**：监控网络连通性，及时处理分区情况
8. **时钟同步**：确保集群节点时钟同步（NTP）
9. **配置一致性**：所有节点使用相同的配置参数
10. **弃用类**：`InMemoryRaftRpc` 和 `RaftCluster` 已废弃，请使用 `WebSocketRaftRpc` 和 `WebSocketRaftCluster`

## 扩展到 TCP

要实现基于 TCP 的通信：

```csharp
public class TcpRaftRpc : IRaftRpc
{
    // 实现TCP通信逻辑
    public async Task<VoteResponse> RequestVoteAsync(string targetNodeId, VoteRequest request)
    {
        // TCP通信实现
    }

    public async Task<AppendEntriesResponse> AppendEntriesAsync(string targetNodeId, AppendEntriesRequest request)
    {
        // TCP通信实现
    }
}
```

## 故障排除

### 常见问题

1. **连接失败**：检查端口是否被占用，防火墙设置
2. **选举失败**：确认网络连通性，调整超时时间
3. **日志不一致**：检查节点间时钟同步，网络稳定性
4. **性能问题**：调整心跳间隔，优化消息序列化
5. **服务发现失败**：检查 DNS 配置，网络连通性，服务注册状态
6. **节点无法加入集群**：验证节点地址配置，检查网络路由

### 服务发现故障排除

#### DNS 解析问题

```bash
# 测试 DNS 解析
nslookup node1.cluster.local

# 检查 DNS 服务器配置
cat /etc/resolv.conf
```

#### 静态配置问题

```csharp
// 验证节点地址配置
var address = await serviceDiscovery.GetNodeAddressAsync("node1");
if (address == null)
{
    Console.WriteLine("节点地址未找到，请检查配置");
}
```

#### 网络连通性测试

```bash
# 测试 WebSocket 连接
curl -I -N -H "Connection: Upgrade" \
     -H "Upgrade: websocket" \
     -H "Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==" \
     -H "Sec-WebSocket-Version: 13" \
     ws://node1.cluster.local:8080/raft/
```

### 日志输出

启用详细日志：

```csharp
// 控制台输出网络通信日志
Console.WriteLine($"[WebSocket] 连接到 {targetUri}");
// ... 其他日志

// 服务发现日志
Console.WriteLine($"[ServiceDiscovery] 发现节点 {nodeId}: {address}");
// ... 其他日志
```

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](https://github.com/joesdu/EasilyNET/blob/dev/LICENSE) 文件了解详情。

## 相关链接

- [Raft 论文](https://raft.github.io/raft.pdf)
- [EasilyNET 主页](https://github.com/joesdu/EasilyNET)

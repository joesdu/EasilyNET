using System.Collections.Concurrent;
using EasilyNET.Consensus.Raft.Discovery;
using Microsoft.VisualStudio.Threading;

namespace EasilyNET.Consensus.Raft.Rpc;

/// <summary>
/// StreamJsonRpc Raft 集群管理器
/// </summary>
public class StreamJsonRpcRaftCluster : IDisposable
{
    private readonly ConcurrentDictionary<string, RaftNode> _nodes;
    private readonly ConcurrentDictionary<string, StreamJsonRpcRaftRpc> _rpcs;
    private readonly IServiceDiscovery _serviceDiscovery;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="nodeIds">节点ID列表</param>
    /// <param name="serviceDiscovery">服务发现实例</param>
    public StreamJsonRpcRaftCluster(List<string> nodeIds, IServiceDiscovery serviceDiscovery)
    {
        _nodes = new();
        _rpcs = new();
        _serviceDiscovery = serviceDiscovery;
        JoinableTaskFactory joinableTaskFactory = new(new JoinableTaskContext());
        joinableTaskFactory.RunAsync(() => InitializeClusterAsync(nodeIds)).Join();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _ = StopAsync();
    }

    /// <summary>
    /// 初始化集群
    /// </summary>
    /// <param name="nodeIds">节点ID列表</param>
    private async Task InitializeClusterAsync(List<string> nodeIds)
    {
        foreach (var nodeId in nodeIds)
        {
            var config = new RaftConfig(nodeId, nodeIds);
            var rpc = new StreamJsonRpcRaftRpc(nodeId, _serviceDiscovery);
            var node = new RaftNode(config, rpc);
            _nodes[nodeId] = node;
            _rpcs[nodeId] = rpc;

            // 仅为本节点启动其RPC服务器
            await rpc.AddNodeAsync(nodeId, node);
        }
    }

    /// <summary>
    /// 启动集群
    /// </summary>
    public async Task StartAsync()
    {
        // 启动所有RPC连接（服务器监听）
        var startTasks = _rpcs.Values.Select(rpc => rpc.StartAsync()).ToArray();
        await Task.WhenAll(startTasks);

        // 启动所有节点
        foreach (var node in _nodes.Values)
        {
            node.Start();
        }
    }

    /// <summary>
    /// 停止集群
    /// </summary>
    public async Task StopAsync()
    {
        // 停止所有节点
        foreach (var node in _nodes.Values)
        {
            node.Stop();
        }

        // 停止所有RPC连接
        var stopTasks = _rpcs.Values.Select(rpc => rpc.StopAsync()).ToArray();
        await Task.WhenAll(stopTasks);
    }

    /// <summary>
    /// 获取节点
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>Raft节点</returns>
    public RaftNode? GetNode(string nodeId)
    {
        _nodes.TryGetValue(nodeId, out var node);
        return node;
    }

    /// <summary>
    /// 获取所有节点
    /// </summary>
    /// <returns>节点字典</returns>
    public ConcurrentDictionary<string, RaftNode> GetNodes() => _nodes;

    /// <summary>
    /// 获取领导者节点
    /// </summary>
    /// <returns>领导者节点</returns>
    public RaftNode? GetLeader()
    {
        return _nodes.Values.FirstOrDefault(n => n.State == RaftState.Leader);
    }
}
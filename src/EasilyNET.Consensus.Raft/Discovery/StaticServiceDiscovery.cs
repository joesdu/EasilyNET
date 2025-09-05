using System.Collections.Concurrent;

namespace EasilyNET.Consensus.Raft.Discovery;

/// <summary>
/// 静态配置服务发现实现
/// </summary>
public class StaticServiceDiscovery : IServiceDiscovery
{
    private readonly ConcurrentDictionary<string, NodeAddress> _nodeAddresses;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="nodeAddresses">节点地址映射</param>
    public StaticServiceDiscovery(Dictionary<string, NodeAddress> nodeAddresses)
    {
        _nodeAddresses = new(nodeAddresses);
    }

    /// <summary>
    /// 获取节点地址
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>节点地址，如果不存在则返回null</returns>
    public Task<NodeAddress?> GetNodeAddressAsync(string nodeId)
    {
        _nodeAddresses.TryGetValue(nodeId, out var address);
        return Task.FromResult(address);
    }

    /// <summary>
    /// 获取所有节点地址
    /// </summary>
    /// <returns>所有节点地址映射</returns>
    public Task<Dictionary<string, NodeAddress>> GetAllNodeAddressesAsync() => Task.FromResult(new Dictionary<string, NodeAddress>(_nodeAddresses));

    /// <summary>
    /// 注册节点地址
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="address">节点地址</param>
    public Task RegisterNodeAsync(string nodeId, NodeAddress address)
    {
        _nodeAddresses[nodeId] = address;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 注销节点
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    public Task UnregisterNodeAsync(string nodeId)
    {
        _nodeAddresses.TryRemove(nodeId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 刷新节点地址缓存
    /// </summary>
    public Task RefreshAsync() =>
        // 静态配置不需要刷新
        Task.CompletedTask;
}
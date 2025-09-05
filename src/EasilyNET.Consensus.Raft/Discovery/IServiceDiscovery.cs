namespace EasilyNET.Consensus.Raft.Discovery;

/// <summary>
/// 服务发现接口
/// </summary>
public interface IServiceDiscovery
{
    /// <summary>
    /// 获取节点地址
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>节点地址</returns>
    Task<NodeAddress?> GetNodeAddressAsync(string nodeId);

    /// <summary>
    /// 获取所有节点地址
    /// </summary>
    /// <returns>节点地址字典</returns>
    Task<Dictionary<string, NodeAddress>> GetAllNodeAddressesAsync();

    /// <summary>
    /// 注册节点地址
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="address">节点地址</param>
    Task RegisterNodeAsync(string nodeId, NodeAddress address);

    /// <summary>
    /// 注销节点
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    Task UnregisterNodeAsync(string nodeId);
}
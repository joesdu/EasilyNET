namespace EasilyNET.Consensus.Raft;

/// <summary>
/// Raft 配置类
/// </summary>
public class RaftConfig
{
    /// <summary>
    /// 选举超时时间（毫秒）
    /// </summary>
    public int ElectionTimeoutMs { get; set; } = 150;

    /// <summary>
    /// 心跳间隔时间（毫秒）
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 50;

    /// <summary>
    /// 节点ID
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 集群中的所有节点ID列表
    /// </summary>
    public List<string> ClusterNodes { get; set; } = new();

    /// <summary>
    /// 节点地址映射（节点ID -> 地址信息）
    /// </summary>
    public Dictionary<string, NodeAddress> NodeAddresses { get; set; } = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public RaftConfig()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="clusterNodes">集群节点列表</param>
    public RaftConfig(string nodeId, List<string> clusterNodes)
    {
        NodeId = nodeId;
        ClusterNodes = clusterNodes;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="clusterNodes">集群节点列表</param>
    /// <param name="nodeAddresses">节点地址映射</param>
    public RaftConfig(string nodeId, List<string> clusterNodes, Dictionary<string, NodeAddress> nodeAddresses)
    {
        NodeId = nodeId;
        ClusterNodes = clusterNodes;
        NodeAddresses = nodeAddresses;
    }

    /// <summary>
    /// 获取节点的地址信息
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>节点地址信息，如果不存在则返回null</returns>
    public NodeAddress? GetNodeAddress(string nodeId)
    {
        return NodeAddresses.TryGetValue(nodeId, out var address) ? address : null;
    }

    /// <summary>
    /// 设置节点的地址信息
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="address">地址信息</param>
    public void SetNodeAddress(string nodeId, NodeAddress address)
    {
        NodeAddresses[nodeId] = address;
    }
}
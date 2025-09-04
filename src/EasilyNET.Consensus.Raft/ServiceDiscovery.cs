namespace EasilyNET.Consensus.Raft;

/// <summary>
/// 节点地址信息
/// </summary>
public class NodeAddress
{
    /// <summary>
    /// 主机地址（IP或主机名）
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// WebSocket路径
    /// </summary>
    public string Path { get; set; } = "/raft";

    /// <summary>
    /// 是否使用SSL
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// 获取WebSocket URI
    /// </summary>
    public string WebSocketUri => $"{(UseSSL ? "wss" : "ws")}://{Host}:{Port}{Path}/";

    /// <summary>
    /// 获取HTTP监听前缀
    /// </summary>
    public string HttpPrefix => $"{(UseSSL ? "https" : "http")}://{Host}:{Port}{Path}/";

    /// <summary>
    /// 构造函数
    /// </summary>
    public NodeAddress()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    public NodeAddress(string host, int port)
    {
        Host = host;
        Port = port;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <param name="path">WebSocket路径</param>
    /// <param name="useSSL">是否使用SSL</param>
    public NodeAddress(string host, int port, string path, bool useSSL = false)
    {
        Host = host;
        Port = port;
        Path = path;
        UseSSL = useSSL;
    }
}

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
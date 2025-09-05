using System.Collections.Concurrent;
using System.Net;

namespace EasilyNET.Consensus.Raft.Discovery;

/// <summary>
/// DNS-based 服务发现实现
/// </summary>
public class DnsServiceDiscovery : IServiceDiscovery
{
    private readonly int _defaultPort;
    private readonly string _domainSuffix;
    private readonly ConcurrentDictionary<string, NodeAddress> _nodeAddresses;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="domainSuffix">域名后缀，例如 ".cluster.local"</param>
    /// <param name="defaultPort">默认端口</param>
    public DnsServiceDiscovery(string domainSuffix, int defaultPort)
    {
        _nodeAddresses = new();
        _domainSuffix = domainSuffix;
        _defaultPort = defaultPort;
    }

    /// <summary>
    /// 获取节点地址
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>节点地址</returns>
    public async Task<NodeAddress?> GetNodeAddressAsync(string nodeId)
    {
        // 尝试从缓存获取
        if (_nodeAddresses.TryGetValue(nodeId, out var cachedAddress))
        {
            return cachedAddress;
        }

        // 通过DNS解析
        try
        {
            var hostName = $"{nodeId}{_domainSuffix}";
            var addresses = await Dns.GetHostAddressesAsync(hostName);
            if (addresses.Length > 0)
            {
                var address = new NodeAddress(addresses[0].ToString(), _defaultPort);
                _nodeAddresses[nodeId] = address;
                return address;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DNS解析失败 {nodeId}: {ex.Message}");
        }
        return null;
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
    public async Task RefreshAsync()
    {
        // 清除缓存，强制重新解析
        _nodeAddresses.Clear();

        // 可以在这里实现批量DNS解析逻辑
        await Task.CompletedTask;
    }
}
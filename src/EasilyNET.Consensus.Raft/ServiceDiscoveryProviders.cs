using System.Collections.Concurrent;

namespace EasilyNET.Consensus.Raft;

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
        _nodeAddresses = new ConcurrentDictionary<string, NodeAddress>(nodeAddresses);
    }

    /// <summary>
    /// 获取节点地址
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>节点地址，如果不存在则返回null</returns>
    public Task<NodeAddress?> GetNodeAddressAsync(string nodeId)
    {
        _nodeAddresses.TryGetValue(nodeId, out var address);
        return Task.FromResult<NodeAddress?>(address);
    }

    /// <summary>
    /// 获取所有节点地址
    /// </summary>
    /// <returns>所有节点地址映射</returns>
    public Task<Dictionary<string, NodeAddress>> GetAllNodeAddressesAsync()
    {
        return Task.FromResult(new Dictionary<string, NodeAddress>(_nodeAddresses));
    }

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
    public Task RefreshAsync()
    {
        // 静态配置不需要刷新
        return Task.CompletedTask;
    }
}

/// <summary>
/// DNS-based 服务发现实现
/// </summary>
public class DnsServiceDiscovery : IServiceDiscovery
{
    private readonly ConcurrentDictionary<string, NodeAddress> _nodeAddresses;
    private readonly string _domainSuffix;
    private readonly int _defaultPort;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="domainSuffix">域名后缀，例如 ".cluster.local"</param>
    /// <param name="defaultPort">默认端口</param>
    public DnsServiceDiscovery(string domainSuffix, int defaultPort)
    {
        _nodeAddresses = new ConcurrentDictionary<string, NodeAddress>();
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
            var addresses = await System.Net.Dns.GetHostAddressesAsync(hostName);

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
    public Task<Dictionary<string, NodeAddress>> GetAllNodeAddressesAsync()
    {
        return Task.FromResult(new Dictionary<string, NodeAddress>(_nodeAddresses));
    }

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

/// <summary>
/// 服务发现工厂
/// </summary>
public static class ServiceDiscoveryFactory
{
    /// <summary>
    /// 创建静态配置服务发现
    /// </summary>
    /// <param name="nodeAddresses">节点地址映射</param>
    /// <returns>服务发现实例</returns>
    public static IServiceDiscovery CreateStatic(Dictionary<string, NodeAddress> nodeAddresses)
    {
        return new StaticServiceDiscovery(nodeAddresses);
    }

    /// <summary>
    /// 创建DNS-based服务发现
    /// </summary>
    /// <param name="domainSuffix">域名后缀</param>
    /// <param name="defaultPort">默认端口</param>
    /// <returns>服务发现实例</returns>
    public static IServiceDiscovery CreateDnsBased(string domainSuffix, int defaultPort)
    {
        return new DnsServiceDiscovery(domainSuffix, defaultPort);
    }

    /// <summary>
    /// 创建基于配置文件的静态服务发现
    /// </summary>
    /// <param name="configFilePath">配置文件路径</param>
    /// <returns>服务发现实例</returns>
    public static async Task<IServiceDiscovery> CreateFromConfigFileAsync(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            throw new FileNotFoundException($"配置文件不存在: {configFilePath}");
        }

        var configJson = await File.ReadAllTextAsync(configFilePath);
        var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, NodeAddress>>(configJson)
                     ?? new Dictionary<string, NodeAddress>();

        return new StaticServiceDiscovery(config);
    }
}
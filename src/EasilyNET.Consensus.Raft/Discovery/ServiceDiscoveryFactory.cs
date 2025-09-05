using System.Text.Json;

namespace EasilyNET.Consensus.Raft.Discovery;

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
    public static IServiceDiscovery CreateStatic(Dictionary<string, NodeAddress> nodeAddresses) => new StaticServiceDiscovery(nodeAddresses);

    /// <summary>
    /// 创建DNS-based服务发现
    /// </summary>
    /// <param name="domainSuffix">域名后缀</param>
    /// <param name="defaultPort">默认端口</param>
    /// <returns>服务发现实例</returns>
    public static IServiceDiscovery CreateDnsBased(string domainSuffix, int defaultPort) => new DnsServiceDiscovery(domainSuffix, defaultPort);

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
        var config = JsonSerializer.Deserialize<Dictionary<string, NodeAddress>>(configJson) ?? new Dictionary<string, NodeAddress>();
        return new StaticServiceDiscovery(config);
    }
}
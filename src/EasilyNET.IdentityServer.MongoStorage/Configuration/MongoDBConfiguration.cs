using MongoDB.Driver;

namespace EasilyNET.IdentityServer.MongoStorage.Configuration;

/// <summary>
/// MongoDB配置
/// </summary>
public class MongoDBConfiguration
{
    /// <summary>
    /// 链接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 数据库
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// SSL设置
    /// </summary>
    public SslSettings? SslSettings { get; set; }
}
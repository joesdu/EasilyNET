namespace EasilyNET.Mongo.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">MongoDB resilience options</para>
///     <para xml:lang="zh">MongoDB 弹性配置选项</para>
/// </summary>
public sealed class MongoResilienceOptions
{
    /// <summary>
    ///     <para xml:lang="en">Enable resilience defaults</para>
    ///     <para xml:lang="zh">启用弹性默认值</para>
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Server selection timeout (MongoDB official default: 30s)</para>
    ///     <para xml:lang="zh">服务器选择超时（MongoDB 官方默认: 30s）</para>
    /// </summary>
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     <para xml:lang="en">Connection timeout (recommended: 10-30s for production)</para>
    ///     <para xml:lang="zh">连接超时（生产环境推荐: 10-30s）</para>
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     <para xml:lang="en">Socket timeout (no response threshold)</para>
    ///     <para xml:lang="zh">Socket 超时（无响应阈值）</para>
    /// </summary>
    public TimeSpan SocketTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    ///     <para xml:lang="en">Wait queue timeout for connection pool (MongoDB official default: 2 minutes)</para>
    ///     <para xml:lang="zh">连接池等待队列超时（MongoDB 官方默认: 2 分钟）</para>
    /// </summary>
    public TimeSpan WaitQueueTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     <para xml:lang="en">Heartbeat interval</para>
    ///     <para xml:lang="zh">心跳间隔</para>
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     <para xml:lang="en">Maximum connection pool size</para>
    ///     <para xml:lang="zh">连接池最大连接数</para>
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    ///     <para xml:lang="en">Minimum connection pool size</para>
    ///     <para xml:lang="zh">连接池最小连接数</para>
    /// </summary>
    public int? MinConnectionPoolSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Retry reads when possible</para>
    ///     <para xml:lang="zh">尽可能重试读取</para>
    /// </summary>
    public bool RetryReads { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Retry writes when possible</para>
    ///     <para xml:lang="zh">尽可能重试写入</para>
    /// </summary>
    public bool RetryWrites { get; set; } = true;
}

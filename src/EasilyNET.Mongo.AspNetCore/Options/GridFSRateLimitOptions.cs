namespace EasilyNET.Mongo.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">GridFS upload rate limiting and protection options</para>
///     <para xml:lang="zh">GridFS 上传速率限制和保护选项</para>
/// </summary>
public sealed class GridFSRateLimitOptions
{
    /// <summary>
    ///     <para xml:lang="en">Maximum concurrent chunk uploads per session (default: 10)</para>
    ///     <para xml:lang="zh">每个会话的最大并发块上传数（默认：10）</para>
    /// </summary>
    public int MaxConcurrentChunksPerSession { get; set; } = 10;

    /// <summary>
    ///     <para xml:lang="en">Maximum concurrent upload sessions globally (default: 100)</para>
    ///     <para xml:lang="zh">全局最大并发上传会话数（默认：100）</para>
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 100;

    /// <summary>
    ///     <para xml:lang="en">Maximum chunk size in bytes (default: 10MB)</para>
    ///     <para xml:lang="zh">最大块大小（字节，默认：10MB）</para>
    /// </summary>
    public int MaxChunkSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    ///     <para xml:lang="en">Minimum chunk size in bytes (default: 256KB)</para>
    ///     <para xml:lang="zh">最小块大小（字节，默认：256KB）</para>
    /// </summary>
    public int MinChunkSize { get; set; } = 256 * 1024;

    /// <summary>
    ///     <para xml:lang="en">Maximum file size in bytes (default: 10GB, 0 = unlimited)</para>
    ///     <para xml:lang="zh">最大文件大小（字节，默认：10GB，0 = 无限制）</para>
    /// </summary>
    public long MaxFileSize { get; set; } = 10L * 1024 * 1024 * 1024;

    /// <summary>
    ///     <para xml:lang="en">Upload session expiration in hours (default: 24)</para>
    ///     <para xml:lang="zh">上传会话过期时间（小时，默认：24）</para>
    /// </summary>
    public int SessionExpirationHours { get; set; } = 24;

    /// <summary>
    ///     <para xml:lang="en">Enable rate limiting (default: true)</para>
    ///     <para xml:lang="zh">启用速率限制（默认：true）</para>
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Rate limit window in seconds (default: 1)</para>
    ///     <para xml:lang="zh">速率限制窗口（秒，默认：1）</para>
    /// </summary>
    public int RateLimitWindowSeconds { get; set; } = 1;

    /// <summary>
    ///     <para xml:lang="en">Maximum requests per rate limit window (default: 50)</para>
    ///     <para xml:lang="zh">每个速率限制窗口的最大请求数（默认：50）</para>
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 50;

    /// <summary>
    ///     <para xml:lang="en">Enable server-side hash verification on finalize (default: true)</para>
    ///     <para xml:lang="zh">在完成上传时启用服务端哈希验证（默认：true）</para>
    /// </summary>
    public bool EnableServerHashVerification { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Database write timeout in seconds (default: 30)</para>
    ///     <para xml:lang="zh">数据库写入超时（秒，默认：30）</para>
    /// </summary>
    public int DatabaseWriteTimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     <para xml:lang="en">Enable chunk hash verification (default: true)</para>
    ///     <para xml:lang="zh">启用块哈希验证（默认：true）</para>
    /// </summary>
    public bool EnableChunkHashVerification { get; set; } = true;
}

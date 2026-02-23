namespace EasilyNET.Mongo.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">MongoDB configuration options</para>
///     <para xml:lang="zh">Mongodb配置选项</para>
/// </summary>
public class BasicClientOptions
{
    /// <summary>
    ///     <para xml:lang="en">Resilience options for connection and retry behavior</para>
    ///     <para xml:lang="zh">连接与重试行为的弹性配置</para>
    /// </summary>
    public MongoResilienceOptions Resilience { get; set; } = new();

    /// <summary>
    ///     <para xml:lang="en">Database name <see langword="string" /></para>
    ///     <para xml:lang="zh">数据库名称 <see langword="string" /></para>
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Whether to drop indexes that exist in the database but are not defined in code attributes.
    ///     Default: <see langword="false" /> (safe mode — only logs unmanaged indexes without dropping them).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     是否删除数据库中存在但未在代码特性中定义的索引。
    ///     默认: <see langword="false" />（安全模式 — 仅记录未管理的索引，不删除）。
    ///     </para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         WARNING: Setting this to <see langword="true" /> will drop indexes created manually by DBAs or other tools.
    ///         Use with caution in production environments.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         警告: 设置为 <see langword="true" /> 将删除由 DBA 或其他工具手动创建的索引。
    ///         在生产环境中请谨慎使用。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public bool DropUnmanagedIndexes { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Index name prefixes that are protected from deletion even when <see cref="DropUnmanagedIndexes" /> is <see langword="true" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     即使 <see cref="DropUnmanagedIndexes" /> 为 <see langword="true" />，也不会被删除的索引名称前缀列表。
    ///     </para>
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<string> ProtectedIndexPrefixes { get; } = [];
}
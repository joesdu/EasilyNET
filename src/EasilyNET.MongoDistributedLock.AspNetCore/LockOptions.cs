namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Mongo分布式锁配置
/// </summary>
public sealed class LockOptions
{
    /// <summary>
    /// 数据库名称
    /// </summary>
    public string? DatabaseName { get; set; } = "easily_lock";

    /// <summary>
    /// 释放信号集合名称
    /// </summary>
    public string? SignalCollName { get; set; } = "lock.release.signal";

    /// <summary>
    /// 锁信息集合名称
    /// </summary>
    public string? AcquireCollName { get; set; } = "lock.acquire";

    /// <summary>
    /// 默认文档数量,默认10000个
    /// <remarks>
    ///     <para>
    ///     仅当集合不存在,第一次创建的时候才会生效,若是集合已存在,请手动修改
    ///     </para>
    /// </remarks>
    /// </summary>
    public long? MaxDocument { get; set; } = 10_000;

    /// <summary>
    /// 集合最大存储大小,默认370000KB
    /// <remarks>
    ///     <para>
    ///     仅当集合不存在,第一次创建的时候才会生效,若是集合已存在,请手动修改
    ///     </para>
    /// </remarks>
    /// </summary>
    public long? MaxSize { get; set; } = 370_000;
}

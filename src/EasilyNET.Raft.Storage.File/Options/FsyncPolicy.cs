namespace EasilyNET.Raft.Storage.File.Options;

/// <summary>
///     <para xml:lang="en">Disk flush policy for raft file persistence</para>
///     <para xml:lang="zh">Raft 文件持久化刷盘策略</para>
/// </summary>
public enum FsyncPolicy
{
    /// <summary>
    ///     <para xml:lang="en">Always fsync for each write</para>
    ///     <para xml:lang="zh">每次写入强制刷盘</para>
    /// </summary>
    Always = 0,

    /// <summary>
    ///     <para xml:lang="en">Flush in batch interval</para>
    ///     <para xml:lang="zh">按批次间隔刷盘</para>
    /// </summary>
    Batch = 1,

    /// <summary>
    ///     <para xml:lang="en">Adaptive by write pressure</para>
    ///     <para xml:lang="zh">按写入压力自适应刷盘</para>
    /// </summary>
    Adaptive = 2
}
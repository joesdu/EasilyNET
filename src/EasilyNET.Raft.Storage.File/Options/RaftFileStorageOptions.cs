// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.Raft.Storage.File.Options;

/// <summary>
///     <para xml:lang="en">File storage options for raft persistence</para>
///     <para xml:lang="zh">Raft 文件存储配置</para>
/// </summary>
public sealed class RaftFileStorageOptions
{
    /// <summary>
    ///     <para xml:lang="en">Base directory for raft files</para>
    ///     <para xml:lang="zh">Raft 文件根目录</para>
    /// </summary>
    public string BaseDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "raft-data");

    /// <summary>
    ///     <para xml:lang="en">State file name</para>
    ///     <para xml:lang="zh">状态文件名</para>
    /// </summary>
    public string StateFileName { get; set; } = "state.json";

    /// <summary>
    ///     <para xml:lang="en">WAL file name</para>
    ///     <para xml:lang="zh">WAL 文件名</para>
    /// </summary>
    public string LogFileName { get; set; } = "wal.log";

    /// <summary>
    ///     <para xml:lang="en">Snapshot metadata file name</para>
    ///     <para xml:lang="zh">快照元数据文件名</para>
    /// </summary>
    public string SnapshotMetadataFileName { get; set; } = "snapshot.meta.json";

    /// <summary>
    ///     <para xml:lang="en">Snapshot data file name</para>
    ///     <para xml:lang="zh">快照数据文件名</para>
    /// </summary>
    public string SnapshotDataFileName { get; set; } = "snapshot.bin";

    /// <summary>
    ///     <para xml:lang="en">Fsync strategy</para>
    ///     <para xml:lang="zh">刷盘策略</para>
    /// </summary>
    public FsyncPolicy FsyncPolicy { get; set; } = FsyncPolicy.Always;

    /// <summary>
    ///     <para xml:lang="en">Batch flush interval in milliseconds</para>
    ///     <para xml:lang="zh">批量刷盘间隔（毫秒）</para>
    /// </summary>
    public int BatchFlushIntervalMs { get; set; } = 100;

    /// <summary>
    ///     <para xml:lang="en">Adaptive threshold writes per second</para>
    ///     <para xml:lang="zh">自适应模式每秒写入阈值</para>
    /// </summary>
    public int AdaptiveHighLoadWritesPerSecond { get; set; } = 200;
}
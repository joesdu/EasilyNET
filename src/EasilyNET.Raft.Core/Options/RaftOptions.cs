namespace EasilyNET.Raft.Core.Options;

/// <summary>
///     <para xml:lang="en">Raft runtime options</para>
///     <para xml:lang="zh">Raft 运行配置</para>
/// </summary>
public sealed class RaftOptions
{
    /// <summary>
    ///     <para xml:lang="en">Minimum election timeout in milliseconds</para>
    ///     <para xml:lang="zh">最小选举超时毫秒</para>
    /// </summary>
    public int ElectionTimeoutMinMs { get; set; } = 150;

    /// <summary>
    ///     <para xml:lang="en">Maximum election timeout in milliseconds</para>
    ///     <para xml:lang="zh">最大选举超时毫秒</para>
    /// </summary>
    public int ElectionTimeoutMaxMs { get; set; } = 300;

    /// <summary>
    ///     <para xml:lang="en">Heartbeat interval in milliseconds</para>
    ///     <para xml:lang="zh">心跳间隔毫秒</para>
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 50;

    /// <summary>
    ///     <para xml:lang="en">Maximum entries in one append request</para>
    ///     <para xml:lang="zh">单次 Append 最大条目数</para>
    /// </summary>
    public int MaxEntriesPerAppend { get; set; } = 100;

    /// <summary>
    ///     <para xml:lang="en">Snapshot trigger threshold</para>
    ///     <para xml:lang="zh">快照触发阈值</para>
    /// </summary>
    public int SnapshotThreshold { get; set; } = 10_000;

    /// <summary>
    ///     <para xml:lang="en">Enable pre-vote flow</para>
    ///     <para xml:lang="zh">是否启用 PreVote</para>
    /// </summary>
    public bool EnablePreVote { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Current node id</para>
    ///     <para xml:lang="zh">当前节点 id</para>
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Cluster member ids</para>
    ///     <para xml:lang="zh">集群成员列表</para>
    /// </summary>
    public List<string> ClusterMembers { get; set; } = [];
}

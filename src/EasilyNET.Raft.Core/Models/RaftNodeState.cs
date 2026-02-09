namespace EasilyNET.Raft.Core.Models;

/// <summary>
///     <para xml:lang="en">Mutable raft node state</para>
///     <para xml:lang="zh">Raft 节点状态</para>
/// </summary>
public sealed class RaftNodeState
{
    /// <summary>
    ///     <para xml:lang="en">Node identifier</para>
    ///     <para xml:lang="zh">节点标识</para>
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Known cluster members</para>
    ///     <para xml:lang="zh">集群成员</para>
    /// </summary>
    public required List<string> ClusterMembers { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Current role</para>
    ///     <para xml:lang="zh">当前角色</para>
    /// </summary>
    public RaftRole Role { get; set; } = RaftRole.Follower;

    /// <summary>
    ///     <para xml:lang="en">Current term</para>
    ///     <para xml:lang="zh">当前任期</para>
    /// </summary>
    public long CurrentTerm { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Voted candidate in current term</para>
    ///     <para xml:lang="zh">当前任期投票对象</para>
    /// </summary>
    public string? VotedFor { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Known leader id</para>
    ///     <para xml:lang="zh">当前已知 Leader Id</para>
    /// </summary>
    public string? LeaderId { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Replicated log</para>
    ///     <para xml:lang="zh">复制日志</para>
    /// </summary>
    public List<RaftLogEntry> Log { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Highest committed index</para>
    ///     <para xml:lang="zh">已提交索引</para>
    /// </summary>
    public long CommitIndex { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Highest applied index</para>
    ///     <para xml:lang="zh">已应用索引</para>
    /// </summary>
    public long LastApplied { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Leader next index for each follower</para>
    ///     <para xml:lang="zh">Leader 维护的 nextIndex</para>
    /// </summary>
    public Dictionary<string, long> NextIndex { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Leader matched index for each follower</para>
    ///     <para xml:lang="zh">Leader 维护的 matchIndex</para>
    /// </summary>
    public Dictionary<string, long> MatchIndex { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Votes granted in current election</para>
    ///     <para xml:lang="zh">当前选举已获票数节点集合</para>
    /// </summary>
    public HashSet<string> VotesGranted { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">PreVotes granted in current round</para>
    ///     <para xml:lang="zh">当前预投票已获票数节点集合</para>
    /// </summary>
    public HashSet<string> PreVotesGranted { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Latest local snapshot last included index</para>
    ///     <para xml:lang="zh">本地最新快照包含的最后索引</para>
    /// </summary>
    public long SnapshotLastIncludedIndex { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Latest local snapshot last included term</para>
    ///     <para xml:lang="zh">本地最新快照包含的最后任期</para>
    /// </summary>
    public long SnapshotLastIncludedTerm { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Pending configuration change entry index</para>
    ///     <para xml:lang="zh">待生效成员变更日志索引</para>
    /// </summary>
    public long? PendingConfigurationChangeIndex { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Pending configuration change operation</para>
    ///     <para xml:lang="zh">待生效成员变更类型</para>
    /// </summary>
    public ConfigurationChangeType? PendingConfigurationChangeType { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Pending configuration change node id</para>
    ///     <para xml:lang="zh">待生效成员变更节点 id</para>
    /// </summary>
    public string? PendingConfigurationChangeNodeId { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Current configuration transition phase</para>
    ///     <para xml:lang="zh">当前配置过渡阶段</para>
    /// </summary>
    public ConfigurationTransitionPhase ConfigurationTransitionPhase { get; set; } = ConfigurationTransitionPhase.None;

    /// <summary>
    ///     <para xml:lang="en">Old configuration members used for joint quorum</para>
    ///     <para xml:lang="zh">联合共识旧配置成员</para>
    /// </summary>
    public List<string> OldConfigurationMembers { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">New configuration members used for joint quorum</para>
    ///     <para xml:lang="zh">联合共识新配置成员</para>
    /// </summary>
    public List<string> NewConfigurationMembers { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Joint config log entry index</para>
    ///     <para xml:lang="zh">联合配置日志索引</para>
    /// </summary>
    public long? JointConfigurationIndex { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Final config log entry index</para>
    ///     <para xml:lang="zh">最终配置日志索引</para>
    /// </summary>
    public long? FinalConfigurationIndex { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Gets latest log index (falls back to snapshot metadata when log is empty)</para>
    ///     <para xml:lang="zh">获取最后日志索引（日志为空时回退到快照元数据）</para>
    /// </summary>
    public long LastLogIndex => Log.Count == 0 ? SnapshotLastIncludedIndex : Log[^1].Index;

    /// <summary>
    ///     <para xml:lang="en">Gets latest log term (falls back to snapshot metadata when log is empty)</para>
    ///     <para xml:lang="zh">获取最后日志任期（日志为空时回退到快照元数据）</para>
    /// </summary>
    public long LastLogTerm => Log.Count == 0 ? SnapshotLastIncludedTerm : Log[^1].Term;

    /// <summary>
    ///     <para xml:lang="en">Gets quorum size</para>
    ///     <para xml:lang="zh">获取多数派阈值</para>
    /// </summary>
    public int Quorum => (ClusterMembers.Count / 2) + 1;
}
using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Raft.Core.Messages;

/// <summary>
///     <para xml:lang="en">RequestVote RPC request</para>
///     <para xml:lang="zh">RequestVote 请求</para>
/// </summary>
public sealed record RequestVoteRequest : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Candidate id</para>
    ///     <para xml:lang="zh">候选者 id</para>
    /// </summary>
    public required string CandidateId { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Candidate last log index</para>
    ///     <para xml:lang="zh">候选者最后日志索引</para>
    /// </summary>
    public required long LastLogIndex { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Candidate last log term</para>
    ///     <para xml:lang="zh">候选者最后日志任期</para>
    /// </summary>
    public required long LastLogTerm { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Whether this is a pre-vote request</para>
    ///     <para xml:lang="zh">是否为 PreVote 请求</para>
    /// </summary>
    public bool IsPreVote { get; init; }
}

/// <summary>
///     <para xml:lang="en">RequestVote RPC response</para>
///     <para xml:lang="zh">RequestVote 响应</para>
/// </summary>
public sealed record RequestVoteResponse : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Whether vote granted</para>
    ///     <para xml:lang="zh">是否同意投票</para>
    /// </summary>
    public required bool VoteGranted { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Indicates this response is for pre-vote</para>
    ///     <para xml:lang="zh">标记是否 PreVote 响应</para>
    /// </summary>
    public bool IsPreVote { get; init; }
}

/// <summary>
///     <para xml:lang="en">AppendEntries RPC request</para>
///     <para xml:lang="zh">AppendEntries 请求</para>
/// </summary>
public sealed record AppendEntriesRequest : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Leader id</para>
    ///     <para xml:lang="zh">Leader id</para>
    /// </summary>
    public required string LeaderId { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Previous log index</para>
    ///     <para xml:lang="zh">前置日志索引</para>
    /// </summary>
    public required long PrevLogIndex { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Previous log term</para>
    ///     <para xml:lang="zh">前置日志任期</para>
    /// </summary>
    public required long PrevLogTerm { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Entries to append</para>
    ///     <para xml:lang="zh">待追加日志</para>
    /// </summary>
    public required IReadOnlyList<RaftLogEntry> Entries { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Leader commit index</para>
    ///     <para xml:lang="zh">Leader 提交索引</para>
    /// </summary>
    public required long LeaderCommit { get; init; }
}

/// <summary>
///     <para xml:lang="en">AppendEntries RPC response</para>
///     <para xml:lang="zh">AppendEntries 响应</para>
/// </summary>
public sealed record AppendEntriesResponse : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Whether append succeeded</para>
    ///     <para xml:lang="zh">是否成功</para>
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Highest replicated index on follower</para>
    ///     <para xml:lang="zh">Follower 已匹配最高索引</para>
    /// </summary>
    public required long MatchIndex { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Conflict term for fast rollback</para>
    ///     <para xml:lang="zh">冲突任期（快速回退）</para>
    /// </summary>
    public long? ConflictTerm { get; init; }

    /// <summary>
    ///     <para xml:lang="en">First index of conflict term</para>
    ///     <para xml:lang="zh">冲突任期第一索引</para>
    /// </summary>
    public long? ConflictIndex { get; init; }
}

/// <summary>
///     <para xml:lang="en">InstallSnapshot RPC request</para>
///     <para xml:lang="zh">InstallSnapshot 请求</para>
/// </summary>
public sealed record InstallSnapshotRequest : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Leader id</para>
    ///     <para xml:lang="zh">Leader id</para>
    /// </summary>
    public required string LeaderId { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Snapshot last included index</para>
    ///     <para xml:lang="zh">快照最后包含索引</para>
    /// </summary>
    public required long LastIncludedIndex { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Snapshot last included term</para>
    ///     <para xml:lang="zh">快照最后包含任期</para>
    /// </summary>
    public required long LastIncludedTerm { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Snapshot payload</para>
    ///     <para xml:lang="zh">快照数据</para>
    /// </summary>
    public required byte[] SnapshotData { get; init; }
}

/// <summary>
///     <para xml:lang="en">InstallSnapshot RPC response</para>
///     <para xml:lang="zh">InstallSnapshot 响应</para>
/// </summary>
public sealed record InstallSnapshotResponse : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Whether installation succeeded</para>
    ///     <para xml:lang="zh">是否安装成功</para>
    /// </summary>
    public required bool Success { get; init; }
}

/// <summary>
///     <para xml:lang="en">Election timeout trigger</para>
///     <para xml:lang="zh">选举超时触发</para>
/// </summary>
public sealed record ElectionTimeoutElapsed : RaftMessage;

/// <summary>
///     <para xml:lang="en">Heartbeat timeout trigger</para>
///     <para xml:lang="zh">心跳超时触发</para>
/// </summary>
public sealed record HeartbeatTimeoutElapsed : RaftMessage;

/// <summary>
///     <para xml:lang="en">Client command request</para>
///     <para xml:lang="zh">客户端命令请求</para>
/// </summary>
public sealed record ClientCommandRequest : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Command payload</para>
    ///     <para xml:lang="zh">命令负载</para>
    /// </summary>
    public required byte[] Command { get; init; }
}

/// <summary>
///     <para xml:lang="en">Linearizable read-index request</para>
///     <para xml:lang="zh">线性一致 ReadIndex 请求</para>
/// </summary>
public sealed record ReadIndexRequest : RaftMessage;

/// <summary>
///     <para xml:lang="en">Linearizable read-index response</para>
///     <para xml:lang="zh">线性一致 ReadIndex 响应</para>
/// </summary>
public sealed record ReadIndexResponse : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Whether request succeeded</para>
    ///     <para xml:lang="zh">是否成功</para>
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Current read index</para>
    ///     <para xml:lang="zh">当前可读索引</para>
    /// </summary>
    public required long ReadIndex { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Known leader id</para>
    ///     <para xml:lang="zh">已知 Leader Id</para>
    /// </summary>
    public string? LeaderId { get; init; }
}

/// <summary>
///     <para xml:lang="en">Single-node membership change request</para>
///     <para xml:lang="zh">单节点成员变更请求</para>
/// </summary>
public sealed record ConfigurationChangeRequest : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Change operation</para>
    ///     <para xml:lang="zh">变更操作</para>
    /// </summary>
    public required ConfigurationChangeType ChangeType { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Target node id</para>
    ///     <para xml:lang="zh">目标节点 id</para>
    /// </summary>
    public required string TargetNodeId { get; init; }
}

/// <summary>
///     <para xml:lang="en">Membership change response</para>
///     <para xml:lang="zh">成员变更响应</para>
/// </summary>
public sealed record ConfigurationChangeResponse : RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Whether request accepted</para>
    ///     <para xml:lang="zh">是否接受</para>
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Whether the configuration change has been committed by the cluster</para>
    ///     <para xml:lang="zh">配置变更是否已被集群提交</para>
    /// </summary>
    public bool Committed { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Failure reason</para>
    ///     <para xml:lang="zh">失败原因</para>
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Log index of the pending configuration change entry (used for commit tracking)</para>
    ///     <para xml:lang="zh">待提交配置变更日志索引（用于提交追踪）</para>
    /// </summary>
    public long? PendingIndex { get; init; }
}
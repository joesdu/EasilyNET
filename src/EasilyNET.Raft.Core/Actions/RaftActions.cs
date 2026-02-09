using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Raft.Core.Actions;

/// <summary>
///     <para xml:lang="en">Base raft side-effect action</para>
///     <para xml:lang="zh">Raft 副作用动作基类</para>
/// </summary>
public abstract record RaftAction;

/// <summary>
///     <para xml:lang="en">Send raft message to target node</para>
///     <para xml:lang="zh">发送消息到目标节点</para>
/// </summary>
/// <param name="TargetNodeId">
///     <para xml:lang="en">Target node id</para>
///     <para xml:lang="zh">目标节点</para>
/// </param>
/// <param name="Message">
///     <para xml:lang="en">Message payload</para>
///     <para xml:lang="zh">消息体</para>
/// </param>
public sealed record SendMessageAction(string TargetNodeId, RaftMessage Message) : RaftAction;

/// <summary>
///     <para xml:lang="en">Persist current term/vote state</para>
///     <para xml:lang="zh">持久化任期与投票状态</para>
/// </summary>
/// <param name="Term">
///     <para xml:lang="en">Current term</para>
///     <para xml:lang="zh">当前任期</para>
/// </param>
/// <param name="VotedFor">
///     <para xml:lang="en">Voted candidate id</para>
///     <para xml:lang="zh">投票对象</para>
/// </param>
public sealed record PersistStateAction(long Term, string? VotedFor) : RaftAction;

/// <summary>
///     <para xml:lang="en">Persist appended entries</para>
///     <para xml:lang="zh">持久化日志条目</para>
/// </summary>
/// <param name="Entries">
///     <para xml:lang="en">Entries to persist</para>
///     <para xml:lang="zh">待持久化条目</para>
/// </param>
public sealed record PersistEntriesAction(IReadOnlyList<RaftLogEntry> Entries) : RaftAction;

/// <summary>
///     <para xml:lang="en">Truncate log suffix from the given index (inclusive) before appending new entries</para>
///     <para xml:lang="zh">从指定索引（含）截断日志后缀，用于冲突日志覆盖前的持久化截断</para>
/// </summary>
/// <param name="FromIndexInclusive">
///     <para xml:lang="en">First index to truncate (inclusive)</para>
///     <para xml:lang="zh">截断起始索引（含）</para>
/// </param>
public sealed record TruncateLogSuffixAction(long FromIndexInclusive) : RaftAction;

/// <summary>
///     <para xml:lang="en">Apply entries to state machine</para>
///     <para xml:lang="zh">应用日志到状态机</para>
/// </summary>
/// <param name="Entries">
///     <para xml:lang="en">Entries to apply</para>
///     <para xml:lang="zh">待应用日志</para>
/// </param>
public sealed record ApplyToStateMachineAction(IReadOnlyList<RaftLogEntry> Entries) : RaftAction;

/// <summary>
///     <para xml:lang="en">Persist snapshot payload</para>
///     <para xml:lang="zh">持久化快照</para>
/// </summary>
/// <param name="LastIncludedIndex">
///     <para xml:lang="en">Snapshot last included index</para>
///     <para xml:lang="zh">快照最后包含索引</para>
/// </param>
/// <param name="LastIncludedTerm">
///     <para xml:lang="en">Snapshot last included term</para>
///     <para xml:lang="zh">快照最后包含任期</para>
/// </param>
/// <param name="SnapshotData">
///     <para xml:lang="en">Snapshot bytes</para>
///     <para xml:lang="zh">快照字节</para>
/// </param>
public sealed record TakeSnapshotAction(long LastIncludedIndex, long LastIncludedTerm, byte[] SnapshotData) : RaftAction;

/// <summary>
///     <para xml:lang="en">Reset election timer</para>
///     <para xml:lang="zh">重置选举计时器</para>
/// </summary>
public sealed record ResetElectionTimerAction : RaftAction;

/// <summary>
///     <para xml:lang="en">Reset heartbeat timer</para>
///     <para xml:lang="zh">重置心跳计时器</para>
/// </summary>
public sealed record ResetHeartbeatTimerAction : RaftAction;

/// <summary>
///     <para xml:lang="en">Instructs the runtime to send an InstallSnapshot RPC to a lagging peer</para>
///     <para xml:lang="zh">指示运行时向落后的 Follower 发送 InstallSnapshot RPC</para>
/// </summary>
/// <param name="TargetNodeId">
///     <para xml:lang="en">Target peer node id</para>
///     <para xml:lang="zh">目标节点 id</para>
/// </param>
public sealed record SendSnapshotToPeerAction(string TargetNodeId) : RaftAction;
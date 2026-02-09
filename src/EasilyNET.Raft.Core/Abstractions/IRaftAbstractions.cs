using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Raft.Core.Abstractions;

/// <summary>
///     <para xml:lang="en">Transport abstraction for raft messages</para>
///     <para xml:lang="zh">Raft 传输抽象</para>
/// </summary>
public interface IRaftTransport
{
    /// <summary>
    ///     <para xml:lang="en">Sends message to target node</para>
    ///     <para xml:lang="zh">发送消息到目标节点</para>
    /// </summary>
    Task<RaftMessage?> SendAsync(string targetNodeId, RaftMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
///     <para xml:lang="en">Persistent term/vote store</para>
///     <para xml:lang="zh">term/votedFor 持久化</para>
/// </summary>
public interface IStateStore
{
    /// <summary>
    ///     <para xml:lang="en">Loads persisted state</para>
    ///     <para xml:lang="zh">读取持久化状态</para>
    /// </summary>
    Task<(long Term, string? VotedFor)> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Saves persisted state atomically</para>
    ///     <para xml:lang="zh">原子保存状态</para>
    /// </summary>
    Task SaveAsync(long term, string? votedFor, CancellationToken cancellationToken = default);
}

/// <summary>
///     <para xml:lang="en">Persistent raft log store</para>
///     <para xml:lang="zh">Raft 日志存储抽象</para>
/// </summary>
public interface ILogStore
{
    /// <summary>
    ///     <para xml:lang="en">Gets all entries</para>
    ///     <para xml:lang="zh">获取全部日志条目</para>
    /// </summary>
    Task<IReadOnlyList<RaftLogEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Appends entries</para>
    ///     <para xml:lang="zh">追加日志条目</para>
    /// </summary>
    Task AppendAsync(IReadOnlyList<RaftLogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Truncates log to last included index</para>
    ///     <para xml:lang="zh">日志截断</para>
    /// </summary>
    Task TruncateSuffixAsync(long fromIndexInclusive, CancellationToken cancellationToken = default);
}

/// <summary>
///     <para xml:lang="en">Snapshot persistence abstraction</para>
///     <para xml:lang="zh">快照存储抽象</para>
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    ///     <para xml:lang="en">Loads latest snapshot metadata and payload</para>
    ///     <para xml:lang="zh">读取最新快照</para>
    /// </summary>
    Task<(long LastIncludedIndex, long LastIncludedTerm, byte[]? Data)> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Saves snapshot payload and metadata</para>
    ///     <para xml:lang="zh">写入快照</para>
    /// </summary>
    Task SaveAsync(long lastIncludedIndex, long lastIncludedTerm, byte[] data, CancellationToken cancellationToken = default);
}

/// <summary>
///     <para xml:lang="en">Replicated state machine abstraction</para>
///     <para xml:lang="zh">状态机抽象</para>
/// </summary>
public interface IStateMachine
{
    /// <summary>
    ///     <para xml:lang="en">Applies committed entries</para>
    ///     <para xml:lang="zh">应用已提交日志</para>
    /// </summary>
    Task ApplyAsync(IReadOnlyList<RaftLogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Creates snapshot from state machine</para>
    ///     <para xml:lang="zh">创建状态机快照</para>
    /// </summary>
    Task<byte[]> CreateSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Restores state machine from snapshot</para>
    ///     <para xml:lang="zh">从快照恢复状态机</para>
    /// </summary>
    Task RestoreSnapshotAsync(byte[] snapshotData, CancellationToken cancellationToken = default);
}

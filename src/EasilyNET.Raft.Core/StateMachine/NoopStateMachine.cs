using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Raft.Core.StateMachine;

/// <summary>
///     <para xml:lang="en">No-op state machine implementation</para>
///     <para xml:lang="zh">空实现状态机</para>
/// </summary>
public sealed class NoopStateMachine : IStateMachine
{
    /// <inheritdoc />
    public Task ApplyAsync(IReadOnlyList<RaftLogEntry> entries, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<byte[]> CreateSnapshotAsync(CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<byte>());

    /// <inheritdoc />
    public Task RestoreSnapshotAsync(byte[] snapshotData, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

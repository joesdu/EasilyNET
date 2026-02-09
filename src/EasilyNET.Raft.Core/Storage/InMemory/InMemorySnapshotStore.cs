using EasilyNET.Raft.Core.Abstractions;

namespace EasilyNET.Raft.Core.Storage.InMemory;

/// <summary>
///     <para xml:lang="en">In-memory implementation for snapshot store</para>
///     <para xml:lang="zh">内存版快照存储</para>
/// </summary>
public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private byte[]? _data;
    private long _lastIncludedIndex;
    private long _lastIncludedTerm;

    /// <inheritdoc />
    public Task<(long LastIncludedIndex, long LastIncludedTerm, byte[]? Data)> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult((_lastIncludedIndex, _lastIncludedTerm, _data));

    /// <inheritdoc />
    public Task SaveAsync(long lastIncludedIndex, long lastIncludedTerm, byte[] data, CancellationToken cancellationToken = default)
    {
        _lastIncludedIndex = lastIncludedIndex;
        _lastIncludedTerm = lastIncludedTerm;
        _data = data;
        return Task.CompletedTask;
    }
}
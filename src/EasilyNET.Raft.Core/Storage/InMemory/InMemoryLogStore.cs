using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Raft.Core.Storage.InMemory;

/// <summary>
///     <para xml:lang="en">In-memory implementation for raft log store</para>
///     <para xml:lang="zh">内存版 Raft 日志存储</para>
/// </summary>
public sealed class InMemoryLogStore : ILogStore
{
    private readonly List<RaftLogEntry> _entries = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<RaftLogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<RaftLogEntry>>(_entries.ToArray());
    }

    /// <inheritdoc />
    public Task AppendAsync(IReadOnlyList<RaftLogEntry> entries, CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
        {
            return Task.CompletedTask;
        }
        _entries.AddRange(entries);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TruncateSuffixAsync(long fromIndexInclusive, CancellationToken cancellationToken = default)
    {
        _entries.RemoveAll(x => x.Index >= fromIndexInclusive);
        return Task.CompletedTask;
    }
}

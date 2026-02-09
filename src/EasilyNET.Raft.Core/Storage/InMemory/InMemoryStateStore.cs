using EasilyNET.Raft.Core.Abstractions;

namespace EasilyNET.Raft.Core.Storage.InMemory;

/// <summary>
///     <para xml:lang="en">In-memory implementation for persisted term/vote state</para>
///     <para xml:lang="zh">内存版 term/votedFor 存储</para>
/// </summary>
public sealed class InMemoryStateStore : IStateStore
{
    private long _term;
    private string? _votedFor;

    /// <inheritdoc />
    public Task<(long Term, string? VotedFor)> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((_term, _votedFor));
    }

    /// <inheritdoc />
    public Task SaveAsync(long term, string? votedFor, CancellationToken cancellationToken = default)
    {
        _term = term;
        _votedFor = votedFor;
        return Task.CompletedTask;
    }
}

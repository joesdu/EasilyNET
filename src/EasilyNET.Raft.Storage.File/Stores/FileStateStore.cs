using System.Text.Json;
using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Storage.File.Options;

namespace EasilyNET.Raft.Storage.File.Stores;

/// <summary>
///     <para xml:lang="en">File-based state store for term and vote</para>
///     <para xml:lang="zh">文件版 term/votedFor 存储</para>
/// </summary>
public sealed class FileStateStore(RaftFileStorageOptions options) : IStateStore
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private string StatePath => Path.Combine(options.BaseDirectory, options.StateFileName);

    /// <inheritdoc />
    public async Task<(long Term, string? VotedFor)> LoadAsync(CancellationToken cancellationToken = default)
    {
        EnsureDirectory();
        if (!System.IO.File.Exists(StatePath))
        {
            return (0, null);
        }
        await using var stream = System.IO.File.OpenRead(StatePath);
        var dto = await JsonSerializer.DeserializeAsync<StateDto>(stream, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return dto is null ? (0, null) : (dto.Term, dto.VotedFor);
    }

    /// <inheritdoc />
    public async Task SaveAsync(long term, string? votedFor, CancellationToken cancellationToken = default)
    {
        EnsureDirectory();
        var temp = StatePath + ".tmp";
        var payload = new StateDto(term, votedFor);
        await using (var stream = System.IO.File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, payload, _serializerOptions, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            // Raft 要求 term/votedFor 必须在响应 RPC 前持久化到稳定存储，否则崩溃后可能重复投票导致脑裂
            // Raft requires term/votedFor to be durable before responding to RPCs to prevent split-brain after crash
            stream.Flush(true);
        }
        System.IO.File.Move(temp, StatePath, true);
    }

    private void EnsureDirectory() => Directory.CreateDirectory(options.BaseDirectory);

    private sealed record StateDto(long Term, string? VotedFor);
}
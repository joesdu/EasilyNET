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
    private readonly FlushPolicyDecider _flushDecider = new(options);
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
            if (_flushDecider.ShouldFlushNow())
            {
                stream.Flush(flushToDisk: true);
            }
        }

        System.IO.File.Move(temp, StatePath, overwrite: true);
    }

    private void EnsureDirectory() => Directory.CreateDirectory(options.BaseDirectory);

    private sealed record StateDto(long Term, string? VotedFor);
}

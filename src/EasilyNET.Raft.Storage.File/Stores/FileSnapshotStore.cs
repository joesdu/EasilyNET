using System.Text.Json;
using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Storage.File.Options;

namespace EasilyNET.Raft.Storage.File.Stores;

/// <summary>
///     <para xml:lang="en">File-based snapshot store</para>
///     <para xml:lang="zh">文件版快照存储</para>
/// </summary>
public sealed class FileSnapshotStore(RaftFileStorageOptions options) : ISnapshotStore
{
    private readonly FlushPolicyDecider _flushDecider = new(options);
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private string MetadataPath => Path.Combine(options.BaseDirectory, options.SnapshotMetadataFileName);
    private string SnapshotPath => Path.Combine(options.BaseDirectory, options.SnapshotDataFileName);

    /// <inheritdoc />
    public async Task<(long LastIncludedIndex, long LastIncludedTerm, byte[]? Data)> LoadAsync(CancellationToken cancellationToken = default)
    {
        EnsureDirectory();
        if (!System.IO.File.Exists(MetadataPath) || !System.IO.File.Exists(SnapshotPath))
        {
            return (0, 0, null);
        }

        await using var metadataStream = System.IO.File.OpenRead(MetadataPath);
        var metadata = await JsonSerializer.DeserializeAsync<SnapshotMetadata>(metadataStream, _serializerOptions, cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            return (0, 0, null);
        }

        var data = await System.IO.File.ReadAllBytesAsync(SnapshotPath, cancellationToken).ConfigureAwait(false);
        return (metadata.LastIncludedIndex, metadata.LastIncludedTerm, data);
    }

    /// <inheritdoc />
    public async Task SaveAsync(long lastIncludedIndex, long lastIncludedTerm, byte[] data, CancellationToken cancellationToken = default)
    {
        EnsureDirectory();
        var snapshotTemp = SnapshotPath + ".tmp";
        var metadataTemp = MetadataPath + ".tmp";

        await System.IO.File.WriteAllBytesAsync(snapshotTemp, data, cancellationToken).ConfigureAwait(false);
        if (_flushDecider.ShouldFlushNow())
        {
            using var fs = new FileStream(snapshotTemp, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            fs.Flush(flushToDisk: true);
        }

        var metadata = new SnapshotMetadata(lastIncludedIndex, lastIncludedTerm);
        await using (var stream = System.IO.File.Create(metadataTemp))
        {
            await JsonSerializer.SerializeAsync(stream, metadata, _serializerOptions, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);
        }

        System.IO.File.Move(snapshotTemp, SnapshotPath, overwrite: true);
        System.IO.File.Move(metadataTemp, MetadataPath, overwrite: true);
    }

    private void EnsureDirectory() => Directory.CreateDirectory(options.BaseDirectory);

    private sealed record SnapshotMetadata(long LastIncludedIndex, long LastIncludedTerm);
}

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
            await using var fs = new FileStream(snapshotTemp, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            fs.Flush(true);
        }
        var metadata = new SnapshotMetadata(lastIncludedIndex, lastIncludedTerm);
        await using (var stream = System.IO.File.Create(metadataTemp))
        {
            await JsonSerializer.SerializeAsync(stream, metadata, _serializerOptions, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Flush(true);
        }
        // 先替换元数据，再替换数据文件。若崩溃发生在两次 Move 之间：
        // 新元数据 + 旧数据 → 元数据指向更高索引但数据是旧的，恢复时 Leader 会通过 AppendEntries 补齐缺失条目，安全。
        // 反过来（先数据后元数据）会导致旧元数据 + 新数据 → 元数据声称覆盖到旧索引但数据已是新快照，数据损坏。
        // Write metadata first, then data. If crash between moves:
        // new metadata + old data → safe (leader will send missing entries via AppendEntries)
        // old metadata + new data → UNSAFE (metadata claims old index but data is new snapshot → corruption)
        System.IO.File.Move(metadataTemp, MetadataPath, true);
        System.IO.File.Move(snapshotTemp, SnapshotPath, true);
    }

    private void EnsureDirectory() => Directory.CreateDirectory(options.BaseDirectory);

    private sealed record SnapshotMetadata(long LastIncludedIndex, long LastIncludedTerm);
}
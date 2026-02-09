using System.Text;
using System.Text.Json;
using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Core.Models;
using EasilyNET.Raft.Storage.File.Options;

namespace EasilyNET.Raft.Storage.File.Stores;

/// <summary>
///     <para xml:lang="en">File-based WAL log store</para>
///     <para xml:lang="zh">文件版 WAL 日志存储</para>
/// </summary>
public sealed class FileLogStore(RaftFileStorageOptions options) : ILogStore
{
    private readonly FlushPolicyDecider _flushDecider = new(options);
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private string LogPath => Path.Combine(options.BaseDirectory, options.LogFileName);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RaftLogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        EnsureDirectory();
        if (!System.IO.File.Exists(LogPath))
        {
            return [];
        }

        var list = new List<RaftLogEntry>();
        await using var stream = System.IO.File.Open(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            var dto = JsonSerializer.Deserialize<LogEntryDto>(line, _serializerOptions);
            if (dto is null)
            {
                continue;
            }
            list.Add(new(dto.Index, dto.Term, dto.Command));
        }
        return list;
    }

    /// <inheritdoc />
    public async Task AppendAsync(IReadOnlyList<RaftLogEntry> entries, CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
        {
            return;
        }

        EnsureDirectory();
        await using var stream = System.IO.File.Open(LogPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        foreach (var entry in entries)
        {
            var dto = new LogEntryDto(entry.Index, entry.Term, entry.Command);
            var line = JsonSerializer.Serialize(dto, _serializerOptions);
            await writer.WriteLineAsync(line).ConfigureAwait(false);
        }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        if (_flushDecider.ShouldFlushNow())
        {
            stream.Flush(flushToDisk: true);
        }
    }

    /// <inheritdoc />
    public async Task TruncateSuffixAsync(long fromIndexInclusive, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        var remaining = all.Where(x => x.Index < fromIndexInclusive).ToArray();

        EnsureDirectory();
        var temp = LogPath + ".tmp";
        await using (var stream = System.IO.File.Create(temp))
        await using (var writer = new StreamWriter(stream, Encoding.UTF8))
        {
            foreach (var entry in remaining)
            {
                var dto = new LogEntryDto(entry.Index, entry.Term, entry.Command);
                var line = JsonSerializer.Serialize(dto, _serializerOptions);
                await writer.WriteLineAsync(line).ConfigureAwait(false);
            }
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);
        }

        System.IO.File.Move(temp, LogPath, overwrite: true);
    }

    private void EnsureDirectory() => Directory.CreateDirectory(options.BaseDirectory);

    private sealed record LogEntryDto(long Index, long Term, byte[] Command);
}

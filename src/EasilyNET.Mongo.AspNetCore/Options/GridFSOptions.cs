using MongoDB.Driver.GridFS;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Mongo.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">GridFS bucket configuration options</para>
///     <para xml:lang="zh">GridFS 存储桶配置选项</para>
/// </summary>
public sealed class GridFSOptions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     The name of the GridFS bucket. Defaults to <c>"fs"</c>.
    ///     This determines the prefix for the chunks and files collections (e.g., <c>fs.files</c> and <c>fs.chunks</c>).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     GridFS 存储桶的名称。默认为 <c>"fs"</c>。
    ///     这决定了 chunks 和 files 集合的前缀（例如 <c>fs.files</c> 和 <c>fs.chunks</c>）。
    ///     </para>
    /// </summary>
    public string BucketName { get; set; } = "fs";

    /// <summary>
    ///     <para xml:lang="en">
    ///     The size of each chunk in bytes. Defaults to 261120 (255 KB).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     每个块的大小（字节）。默认为 261120（255 KB）。
    ///     </para>
    /// </summary>
    public int ChunkSizeBytes { get; set; } = 261120;

    /// <summary>
    ///     <para xml:lang="en">Convert to <see cref="GridFSBucketOptions" /></para>
    ///     <para xml:lang="zh">转换为 <see cref="GridFSBucketOptions" /></para>
    /// </summary>
    internal GridFSBucketOptions ToGridFSBucketOptions() =>
        new()
        {
            BucketName = BucketName,
            ChunkSizeBytes = ChunkSizeBytes
        };
}
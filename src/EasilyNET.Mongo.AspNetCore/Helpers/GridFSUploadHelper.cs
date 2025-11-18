using MongoDB.Bson;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">GridFS upload optimization helper</para>
///     <para xml:lang="zh">GridFS 上传优化辅助类</para>
/// </summary>
public static class GridFSUploadHelper
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Upload file with optimized buffer size based on file size. Automatically adjusts chunk size for optimal performance.
    ///     </para>
    ///     <para xml:lang="zh">根据文件大小使用优化的缓冲区上传文件。自动调整块大小以获得最佳性能。</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS 存储桶</para>
    /// </param>
    /// <param name="filename">
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </param>
    /// <param name="source">
    ///     <para xml:lang="en">Source stream</para>
    ///     <para xml:lang="zh">源流</para>
    /// </param>
    /// <param name="options">
    ///     <para xml:lang="en">Upload options (optional)</para>
    ///     <para xml:lang="zh">上传选项(可选)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">File ObjectId</para>
    ///     <para xml:lang="zh">文件 ObjectId</para>
    /// </returns>
    public static async Task<ObjectId> UploadOptimizedAsync(
        IGridFSBucket bucket,
        string filename,
        Stream source,
        GridFSUploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // 根据流长度优化块大小
        var chunkSize = GetOptimalChunkSize(source.Length);
        var uploadOptions = options ?? new GridFSUploadOptions();
        uploadOptions.ChunkSizeBytes = chunkSize;
        return await bucket.UploadFromStreamAsync(filename, source, uploadOptions, cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Upload multiple files in parallel with optimized settings. Suitable for batch upload scenarios.
    ///     </para>
    ///     <para xml:lang="zh">并行上传多个文件并使用优化设置。适用于批量上传场景。</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS 存储桶</para>
    /// </param>
    /// <param name="files">
    ///     <para xml:lang="en">File collection (filename, stream, metadata)</para>
    ///     <para xml:lang="zh">文件集合(文件名,流,元数据)</para>
    /// </param>
    /// <param name="maxDegreeOfParallelism">
    ///     <para xml:lang="en">Maximum degree of parallelism (0 for CPU count)</para>
    ///     <para xml:lang="zh">最大并行度(0 表示 CPU 数量)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Collection of file ObjectIds</para>
    ///     <para xml:lang="zh">文件 ObjectId 集合</para>
    /// </returns>
    public static async Task<IReadOnlyList<ObjectId>> UploadManyAsync(
        IGridFSBucket bucket,
        IEnumerable<(string Filename, Stream Source, Dictionary<string, object>? Metadata)> files,
        int maxDegreeOfParallelism = 0,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        var results = new ObjectId[fileList.Count];
        var dop = maxDegreeOfParallelism <= 0 ? Environment.ProcessorCount : maxDegreeOfParallelism;
        await Parallel.ForEachAsync(fileList.Select((file, index) => (file, index)),
            new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken },
            async (item, ct) =>
            {
                var (file, index) = item;
                var chunkSize = GetOptimalChunkSize(file.Source.Length);
                var options = new GridFSUploadOptions
                {
                    ChunkSizeBytes = chunkSize,
                    Metadata = file.Metadata is not null ? new(file.Metadata) : null
                };
                results[index] = await bucket.UploadFromStreamAsync(file.Filename, file.Source, options, ct);
            });
        return results;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Calculate optimal chunk size based on file size. Larger files use larger chunks for better performance.
    ///     </para>
    ///     <para xml:lang="zh">根据文件大小计算最佳块大小。较大文件使用较大块以获得更好性能。</para>
    /// </summary>
    /// <param name="fileSize">
    ///     <para xml:lang="en">File size in bytes</para>
    ///     <para xml:lang="zh">文件大小(字节)</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Optimal chunk size in bytes</para>
    ///     <para xml:lang="zh">最佳块大小(字节)</para>
    /// </returns>
    public static int GetOptimalChunkSize(long fileSize)
    {
        return fileSize switch
        {
            // 小文件 < 1MB: 使用 64KB 块
            < 1024 * 1024 => 64 * 1024,
            // 中等文件 1MB - 10MB: 使用 255KB 块(GridFS 默认)
            < 10 * 1024 * 1024 => 255 * 1024,
            // 大文件 10MB - 100MB: 使用 512KB 块
            < 100 * 1024 * 1024 => 512 * 1024,
            // 超大文件 >= 100MB: 使用 1MB 块
            _ => 1024 * 1024
        };
    }
}
using System.IO.Compression;
using EasilyNET.Core.Essentials;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.IO;

/// <summary>
///     <para xml:lang="en">Compress/Decompress byte arrays</para>
///     <para xml:lang="zh">压缩/解压 字节数组</para>
/// </summary>
public static class CompressionHelper
{
    /// <summary>
    ///     <para xml:lang="en">Compress</para>
    ///     <para xml:lang="zh">压缩</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">Byte array to compress</para>
    ///     <para xml:lang="zh">要压缩的字节数组</para>
    /// </param>
    public static byte[] Compress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        // 无需线程安全: 方法内局部变量，生命周期完全封闭
        using var memoryStream = new PooledMemoryStream();
        using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
        {
            deflateStream.Write(data, 0, data.Length);
        }
        return memoryStream.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Compress with specified compression level</para>
    ///     <para xml:lang="zh">使用指定压缩级别进行压缩</para>
    /// </summary>
    public static byte[] Compress(byte[] data, CompressionLevel compressionLevel)
    {
        ArgumentNullException.ThrowIfNull(data);
        // 无需线程安全: 方法内局部变量，生命周期完全封闭
        using var memoryStream = new PooledMemoryStream();
        using (var deflateStream = new DeflateStream(memoryStream, compressionLevel, true))
        {
            deflateStream.Write(data, 0, data.Length);
        }
        return memoryStream.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Decompress</para>
    ///     <para xml:lang="zh">解压</para>
    /// </summary>
    /// <param name="compressedData">
    ///     <para xml:lang="en">Compressed byte array</para>
    ///     <para xml:lang="zh">压缩的字节数组</para>
    /// </param>
    public static byte[] Decompress(byte[] compressedData)
    {
        ArgumentNullException.ThrowIfNull(compressedData);
        // 无需线程安全: 方法内局部变量，生命周期完全封闭
        using var memoryStream = new PooledMemoryStream(compressedData);
        using var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
        using var decompressedMemoryStream = new PooledMemoryStream();
        deflateStream.CopyTo(decompressedMemoryStream);
        return decompressedMemoryStream.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Compress asynchronously</para>
    ///     <para xml:lang="zh">异步压缩</para>
    /// </summary>
    public static async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        // 无需线程安全: 异步方法内局部变量，生命周期完全封闭
        await using var memoryStream = new PooledMemoryStream();
        await using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
        {
            await deflateStream.WriteAsync(data.AsMemory(), cancellationToken);
        }
        return memoryStream.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Compress asynchronously with specified compression level</para>
    ///     <para xml:lang="zh">使用指定压缩级别进行异步压缩</para>
    /// </summary>
    public static async Task<byte[]> CompressAsync(byte[] data, CompressionLevel compressionLevel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        // 无需线程安全: 异步方法内局部变量，生命周期完全封闭
        await using var memoryStream = new PooledMemoryStream();
        await using (var deflateStream = new DeflateStream(memoryStream, compressionLevel, true))
        {
            await deflateStream.WriteAsync(data.AsMemory(), cancellationToken);
        }
        return memoryStream.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Decompress asynchronously</para>
    ///     <para xml:lang="zh">异步解压</para>
    /// </summary>
    public static async Task<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compressedData);
        // 无需线程安全: 异步方法内局部变量，生命周期完全封闭
        await using var input = new PooledMemoryStream(compressedData);
        await using var deflateStream = new DeflateStream(input, CompressionMode.Decompress);
        await using var output = new PooledMemoryStream();
        await deflateStream.CopyToAsync(output, cancellationToken);
        return output.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Compress from source stream to destination stream (async)</para>
    ///     <para xml:lang="zh">从源流压缩到目标流(异步)</para>
    /// </summary>
    public static async ValueTask CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        await using var deflateStream = new DeflateStream(destination, CompressionMode.Compress, true);
        await source.CopyToAsync(deflateStream, cancellationToken);
        await deflateStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Compress from source stream to destination stream (async, with level)</para>
    ///     <para xml:lang="zh">从源流压缩到目标流(异步, 指定级别)</para>
    /// </summary>
    public static async ValueTask CompressAsync(Stream source, Stream destination, CompressionLevel compressionLevel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        await using var deflateStream = new DeflateStream(destination, compressionLevel, true);
        await source.CopyToAsync(deflateStream, cancellationToken);
        await deflateStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Decompress from source stream to destination stream (async)</para>
    ///     <para xml:lang="zh">从源流解压到目标流(异步)</para>
    /// </summary>
    public static async ValueTask DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        await using var deflateStream = new DeflateStream(source, CompressionMode.Decompress, true);
        await deflateStream.CopyToAsync(destination, cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Compress many payloads concurrently</para>
    ///     <para xml:lang="zh">并行压缩多个字节数组</para>
    /// </summary>
    public static async Task<IReadOnlyList<byte[]>> CompressManyAsync(IEnumerable<byte[]> payloads, CompressionLevel compressionLevel = CompressionLevel.Optimal, int degreeOfParallelism = 0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payloads);
        var list = payloads as IList<byte[]> ?? payloads.ToList();
        if (list.Count == 0)
        {
            return [];
        }
        var results = new byte[list.Count][];
        var dop = degreeOfParallelism <= 0 ? Environment.ProcessorCount : degreeOfParallelism;
        await Parallel.ForEachAsync(Enumerable.Range(0, list.Count), new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken }, async (i, ct) =>
        {
            var data = list[i];
            results[i] = await CompressAsync(data, compressionLevel, ct).ConfigureAwait(false);
        });
        return results;
    }

    /// <summary>
    ///     <para xml:lang="en">Decompress many payloads concurrently</para>
    ///     <para xml:lang="zh">并行解压多个字节数组</para>
    /// </summary>
    public static async Task<IReadOnlyList<byte[]>> DecompressManyAsync(IEnumerable<byte[]> payloads, int degreeOfParallelism = 0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payloads);
        var list = payloads as IList<byte[]> ?? payloads.ToList();
        if (list.Count == 0)
        {
            return [];
        }
        var results = new byte[list.Count][];
        var dop = degreeOfParallelism <= 0 ? Environment.ProcessorCount : degreeOfParallelism;
        await Parallel.ForEachAsync(Enumerable.Range(0, list.Count), new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken }, async (i, ct) =>
        {
            var data = list[i];
            results[i] = await DecompressAsync(data, ct).ConfigureAwait(false);
        });
        return results;
    }

    /// <summary>
    ///     <para xml:lang="en">Compress many stream jobs concurrently</para>
    ///     <para xml:lang="zh">并行压缩多个流任务</para>
    /// </summary>
    public static async Task CompressManyAsync(IEnumerable<(Stream source, Stream destination)> jobs, CompressionLevel compressionLevel = CompressionLevel.Optimal, int degreeOfParallelism = 0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        var list = jobs as IList<(Stream source, Stream destination)> ?? jobs.ToList();
        if (list.Count == 0)
        {
            return;
        }
        var dop = degreeOfParallelism <= 0 ? Environment.ProcessorCount : degreeOfParallelism;
        await Parallel.ForEachAsync(Enumerable.Range(0, list.Count), new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken }, async (i, ct) =>
        {
            var (source, destination) = list[i];
            await CompressAsync(source, destination, compressionLevel, ct).ConfigureAwait(false);
        });
    }

    /// <summary>
    ///     <para xml:lang="en">Decompress many stream jobs concurrently</para>
    ///     <para xml:lang="zh">并行解压多个流任务</para>
    /// </summary>
    public static async Task DecompressManyAsync(IEnumerable<(Stream source, Stream destination)> jobs, int degreeOfParallelism = 0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        var list = jobs as IList<(Stream source, Stream destination)> ?? jobs.ToList();
        if (list.Count == 0)
        {
            return;
        }
        var dop = degreeOfParallelism <= 0 ? Environment.ProcessorCount : degreeOfParallelism;
        await Parallel.ForEachAsync(Enumerable.Range(0, list.Count), new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken }, async (i, ct) =>
        {
            var (source, destination) = list[i];
            await DecompressAsync(source, destination, ct).ConfigureAwait(false);
        });
    }
}
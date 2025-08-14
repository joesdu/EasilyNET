using System.Buffers;
using System.IO.Compression;
using System.Runtime.InteropServices;
using EasilyNET.Core.Essentials;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.IO;

/// <summary>
///     <para xml:lang="en">Zip utility</para>
///     <para xml:lang="zh">Zip工具</para>
/// </summary>
public static class ZipHelper
{
    private static string GetSafeExtractionPath(string destinationDirectory, string entryFullName)
    {
        var fullPath = Path.GetFullPath(Path.Combine(destinationDirectory, entryFullName));
        var destFull = Path.GetFullPath(destinationDirectory);
        // Ensure trailing separator to avoid prefix-matching issues (e.g., C:\\dest vs C:\\destination)
        var destFullWithSep = destFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return !fullPath.StartsWith(destFullWithSep, comparison) ? throw new IOException($"Entry '{entryFullName}' is trying to extract outside of destination directory.") : fullPath;
    }

    /// <summary>
    ///     <para xml:lang="en">Compress a directory into a Zip file</para>
    ///     <para xml:lang="zh">将文件夹压缩为Zip</para>
    /// </summary>
    /// <param name="sourceDirectory">
    ///     <para xml:lang="en">Source directory</para>
    ///     <para xml:lang="zh">源文件夹</para>
    /// </param>
    /// <param name="destinationFile">
    ///     <para xml:lang="en">Destination Zip file</para>
    ///     <para xml:lang="zh">目标Zip文件</para>
    /// </param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void ZipDirectory(string sourceDirectory, string destinationFile)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceDirectory}' does not exist.");
        }
        try
        {
            // Ensure directory exists for destination
            var dir = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            ZipFile.CreateFromDirectory(sourceDirectory, destinationFile);
        }
        catch (Exception ex)
        {
            throw new("Error occurred while zipping directory.", ex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Extract a Zip file to a specified directory</para>
    ///     <para xml:lang="zh">解压缩文件到指定目录</para>
    /// </summary>
    /// <param name="zipFile">
    ///     <para xml:lang="en">Zip file</para>
    ///     <para xml:lang="zh">Zip文件</para>
    /// </param>
    /// <param name="destinationDirectory">
    ///     <para xml:lang="en">Destination directory</para>
    ///     <para xml:lang="zh">目标目录</para>
    /// </param>
    /// <exception cref="FileNotFoundException"></exception>
    public static void UnZipFile(string zipFile, string destinationDirectory)
    {
        if (!File.Exists(zipFile))
        {
            throw new FileNotFoundException($"Zip file '{zipFile}' does not exist.");
        }
        try
        {
            Directory.CreateDirectory(destinationDirectory);
            using var zipArchive = ZipFile.OpenRead(zipFile);
            foreach (var entry in zipArchive.Entries)
            {
                var fullPath = GetSafeExtractionPath(destinationDirectory, entry.FullName);
                var directoryPath = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue; // Directory entry
                }
                using var entryStream = entry.Open();
                using var outStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                entryStream.CopyTo(outStream);
            }
        }
        catch (Exception ex)
        {
            throw new("Error occurred while unzipping file.", ex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Compress a specified file into a Zip file</para>
    ///     <para xml:lang="zh">将指定文件压缩为Zip</para>
    /// </summary>
    /// <param name="sourceFile">
    ///     <para xml:lang="en">Source file</para>
    ///     <para xml:lang="zh">源文件</para>
    /// </param>
    /// <param name="destinationZipFile">
    ///     <para xml:lang="en">Destination Zip file</para>
    ///     <para xml:lang="zh">目标Zip文件</para>
    /// </param>
    public static void ZipFromFile(string sourceFile, string destinationZipFile)
    {
        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException($"Source file '{sourceFile}' does not exist.");
        }
        try
        {
            // Ensure directory exists for destination
            var dir = Path.GetDirectoryName(destinationZipFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            using var zipArchive = ZipFile.Open(destinationZipFile, ZipArchiveMode.Create);
            zipArchive.CreateEntryFromFile(sourceFile, Path.GetFileName(sourceFile));
        }
        catch (Exception ex)
        {
            throw new("Error occurred while zipping file.", ex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Compress a directory asynchronously into a Zip file</para>
    ///     <para xml:lang="zh">异步将文件夹压缩为Zip</para>
    /// </summary>
    public static async Task ZipDirectoryAsync(string sourceDirectory, string destinationFile, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceDirectory}' does not exist.");
        }
        try
        {
            var dir = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }
            await using var fs = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Create, true);
            var basePath = Path.GetFullPath(sourceDirectory);
            foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = Path.GetRelativePath(basePath, file);
                var entry = archive.CreateEntry(relative, compressionLevel);
                await using var entryStream = entry.Open();
                await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await input.CopyToAsync(entryStream, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new("Error occurred while zipping directory (async).", ex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Extract a Zip file asynchronously to a specified directory</para>
    ///     <para xml:lang="zh">异步解压缩文件到指定目录</para>
    /// </summary>
    public static async Task UnZipFileAsync(string zipFile, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(zipFile))
        {
            throw new FileNotFoundException($"Zip file '{zipFile}' does not exist.");
        }
        try
        {
            Directory.CreateDirectory(destinationDirectory);
            await using var fs = new FileStream(zipFile, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read, true);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullPath = GetSafeExtractionPath(destinationDirectory, entry.FullName);
                var directoryPath = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue; // Directory entry
                }
                await using var entryStream = entry.Open();
                await using var outStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await entryStream.CopyToAsync(outStream, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new("Error occurred while unzipping file (async).", ex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Extract a Zip file asynchronously and in parallel to a specified directory</para>
    ///     <para xml:lang="zh">并行异步解压到指定目录</para>
    /// </summary>
    public static async Task UnZipFileParallelAsync(string zipFile, string destinationDirectory, int degreeOfParallelism = 0, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(zipFile))
        {
            throw new FileNotFoundException($"Zip file '{zipFile}' does not exist.");
        }
        try
        {
            Directory.CreateDirectory(destinationDirectory);
            // Get all entry names first using a single open
            List<string> entries;
            await using (var fs = new FileStream(zipFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Read, false))
            {
                entries = archive.Entries.Select(e => e.FullName).ToList();
            }
            if (entries.Count == 0)
            {
                return;
            }
            var dop = degreeOfParallelism <= 0 ? Environment.ProcessorCount : degreeOfParallelism;
            await Parallel.ForEachAsync(entries, new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken }, async (entryName, ct) =>
            {
                // For thread-safety, open the archive per task
                await using var fs2 = new FileStream(zipFile, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var archive2 = new ZipArchive(fs2, ZipArchiveMode.Read, true);
                var entry2 = archive2.GetEntry(entryName);
                if (entry2 is null || string.IsNullOrEmpty(entry2.Name))
                {
                    return;
                }
                var fullPath = GetSafeExtractionPath(destinationDirectory, entry2.FullName);
                var directoryPath = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                await using var entryStream = entry2.Open();
                await using var outStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await entryStream.CopyToAsync(outStream, ct);
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new("Error occurred while unzipping file (parallel async).", ex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Compress a directory asynchronously into a Zip file with safe parallel file handling</para>
    ///     <para xml:lang="zh">并行处理文件(线程安全地共享 ZipArchive)的异步目录压缩</para>
    /// </summary>
    public static async Task ZipDirectoryParallelAsync(string sourceDirectory, string destinationFile, CompressionLevel compressionLevel = CompressionLevel.Optimal, int degreeOfParallelism = 0, long preloadThresholdBytes = 1 * 1024 * 1024, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceDirectory}' does not exist.");
        }
        try
        {
            var dir = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }
            await using var fs = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Create, true);
            using var writeLock = new SemaphoreSlim(1, 1);
            var basePath = Path.GetFullPath(sourceDirectory);
            var files = Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories).ToList();
            var dop = degreeOfParallelism <= 0 ? Environment.ProcessorCount : degreeOfParallelism;
            await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken }, async (file, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                var relative = Path.GetRelativePath(basePath, file);

                // Prefer preload for small files to minimize lock duration
                var fi = new FileInfo(file);
                if (fi is { Exists: true, Length: > 0 } && fi.Length <= preloadThresholdBytes)
                {
                    if (fi.Length > int.MaxValue)
                    {
                        throw new IOException($"File '{file}' is too large to preload into memory (size: {fi.Length} bytes).");
                    }
                    await using var buffer = new PooledMemoryStream(ArrayPool<byte>.Shared, (int)fi.Length);
                    await using (var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        await input.CopyToAsync(buffer, ct);
                    }
                    buffer.Position = 0;
                    await writeLock.WaitAsync(ct);
                    try
                    {
                        var entry = archive.CreateEntry(relative, compressionLevel);
                        await using var entryStream = entry.Open();
                        await buffer.WriteToAsync(entryStream, ct);
                    }
                    finally
                    {
                        writeLock.Release();
                    }
                }
                else
                {
                    // Large file or zero-byte file: stream under lock to avoid sharing issues
                    await writeLock.WaitAsync(ct);
                    try
                    {
                        var entry = archive.CreateEntry(relative, compressionLevel);
                        await using var entryStream = entry.Open();
                        if (fi is { Exists: true, Length: > 0 })
                        {
                            await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                            await input.CopyToAsync(entryStream, ct);
                        }
                        // zero-length entry: nothing to copy, ensure empty entry is created
                    }
                    finally
                    {
                        writeLock.Release();
                    }
                }
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new("Error occurred while zipping directory (parallel async).", ex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Compress a specified file asynchronously into a Zip file</para>
    ///     <para xml:lang="zh">异步将指定文件压缩为Zip</para>
    /// </summary>
    public static async Task ZipFromFileAsync(string sourceFile, string destinationZipFile, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException($"Source file '{sourceFile}' does not exist.");
        }
        try
        {
            var dir = Path.GetDirectoryName(destinationZipFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(destinationZipFile))
            {
                File.Delete(destinationZipFile);
            }
            await using var fs = new FileStream(destinationZipFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Create, true);
            var entry = archive.CreateEntry(Path.GetFileName(sourceFile), compressionLevel);
            await using var entryStream = entry.Open();
            await using var input = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await input.CopyToAsync(entryStream, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new("Error occurred while zipping file (async).", ex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Compress a byte array into a Zip in-memory and return bytes (single entry)</para>
    ///     <para xml:lang="zh">将字节数组压缩到内存Zip并返回字节(单文件)</para>
    /// </summary>
    public static byte[] ZipFromBytes(string entryName, byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryName);
        ArgumentNullException.ThrowIfNull(data);
        using var ms = new PooledMemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry(entryName, compressionLevel);
            using var entryStream = entry.Open();
            entryStream.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Compress bytes to in-memory Zip asynchronously (single entry)</para>
    ///     <para xml:lang="zh">异步将字节数组压缩到内存Zip(单文件)</para>
    /// </summary>
    public static async Task<byte[]> ZipFromBytesAsync(string entryName, byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryName);
        ArgumentNullException.ThrowIfNull(data);
        await using var ms = new PooledMemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry(entryName, compressionLevel);
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(data, 0, data.Length, cancellationToken);
        }
        return ms.ToArray();
    }
}
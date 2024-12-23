using System.IO.Compression;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.IO;

/// <summary>
///     <para xml:lang="en">Zip utility</para>
///     <para xml:lang="zh">Zip工具</para>
/// </summary>
public static class ZipHelper
{
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
            using var zipArchive = ZipFile.OpenRead(zipFile);
            zipArchive.ExtractToDirectory(destinationDirectory);
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
            using var zipArchive = ZipFile.Open(destinationZipFile, ZipArchiveMode.Create);
            zipArchive.CreateEntryFromFile(sourceFile, Path.GetFileName(sourceFile));
        }
        catch (Exception ex)
        {
            throw new("Error occurred while zipping file.", ex);
        }
    }
}
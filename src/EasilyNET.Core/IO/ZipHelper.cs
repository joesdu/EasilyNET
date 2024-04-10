using System.IO.Compression;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.IO;

/// <summary>
/// Zip工具
/// </summary>
public static class ZipHelper
{
    /// <summary>
    /// 将文件夹压缩为Zip
    /// </summary>
    /// <param name="sourceDirectory"></param>
    /// <param name="destinationFile"></param>
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
    /// 解压缩文件到指定目录
    /// </summary>
    /// <param name="zipFile"></param>
    /// <param name="destinationDirectory"></param>
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
    /// 将指定文件压缩为Zip
    /// </summary>
    /// <param name="sourceFile"></param>
    /// <param name="destinationZipFile"></param>
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
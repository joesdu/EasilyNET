using System.IO.Compression;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.IO;

/// <summary>
/// 压缩/解压 字节数组
/// </summary>
public static class CompressionHelper
{
    /// <summary>
    /// 压缩
    /// </summary>
    /// <param name="data">要压缩的字节数组</param>
    /// <returns>压缩后的字节数组</returns>
    public static byte[] Compress(byte[] data)
    {
        using var memoryStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
        {
            deflateStream.Write(data, 0, data.Length);
        }
        return memoryStream.ToArray();
    }

    /// <summary>
    /// 解压
    /// </summary>
    /// <param name="compressedData">压缩的字节数组</param>
    /// <returns>解压后的字节数组</returns>
    public static byte[] Decompress(byte[] compressedData)
    {
        using var memoryStream = new MemoryStream(compressedData);
        using var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
        using var decompressedMemoryStream = new MemoryStream();
        deflateStream.CopyTo(decompressedMemoryStream);
        return decompressedMemoryStream.ToArray();
    }
}
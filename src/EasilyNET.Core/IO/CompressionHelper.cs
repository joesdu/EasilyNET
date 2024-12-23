using System.IO.Compression;

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
    /// <returns>
    ///     <para xml:lang="en">Compressed byte array</para>
    ///     <para xml:lang="zh">压缩后的字节数组</para>
    /// </returns>
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
    ///     <para xml:lang="en">Decompress</para>
    ///     <para xml:lang="zh">解压</para>
    /// </summary>
    /// <param name="compressedData">
    ///     <para xml:lang="en">Compressed byte array</para>
    ///     <para xml:lang="zh">压缩的字节数组</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Decompressed byte array</para>
    ///     <para xml:lang="zh">解压后的字节数组</para>
    /// </returns>
    public static byte[] Decompress(byte[] compressedData)
    {
        using var memoryStream = new MemoryStream(compressedData);
        using var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
        using var decompressedMemoryStream = new MemoryStream();
        deflateStream.CopyTo(decompressedMemoryStream);
        return decompressedMemoryStream.ToArray();
    }
}
using System.Security.Cryptography;
using System.Text;

namespace EasilyNET.Security;

internal static class Md5
{
    /// <summary>
    ///     <para xml:lang="en">Get 16-character length MD5 uppercase string</para>
    ///     <para xml:lang="zh">获取16位长度的MD5大写字符串</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">Input string</para>
    ///     <para xml:lang="zh">输入字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">16-character length MD5 uppercase string</para>
    ///     <para xml:lang="zh">16位长度的MD5大写字符串</para>
    /// </returns>
    internal static string To16MD5(this string value) => value.To32MD5().Substring(8, 16);

    /// <summary>
    ///     <para xml:lang="en">Get 32-character length MD5 uppercase string</para>
    ///     <para xml:lang="zh">获取32位长度的MD5大写字符串</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">Input string</para>
    ///     <para xml:lang="zh">输入字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">32-character length MD5 uppercase string</para>
    ///     <para xml:lang="zh">32位长度的MD5大写字符串</para>
    /// </returns>
    internal static string To32MD5(this string value)
    {
        // 计算所需的最大字节数
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        var utf8Bytes = maxByteCount <= 256 ? stackalloc byte[maxByteCount] : new byte[maxByteCount];
        Span<byte> hashBytes = stackalloc byte[MD5.HashSizeInBytes];
        // 将字符串编码为 UTF-8 字节
        var byteCount = Encoding.UTF8.GetBytes(value, utf8Bytes);
        // 计算 MD5 哈希
        MD5.HashData(utf8Bytes[..byteCount], hashBytes);
        // 将哈希字节转换为十六进制字符串
        return Convert.ToHexString(hashBytes);
    }
}
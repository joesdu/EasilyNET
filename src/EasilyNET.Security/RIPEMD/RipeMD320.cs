using System.Text;
using Org.BouncyCastle.Crypto.Digests;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">RIPEMD-320 algorithm</para>
///     <para xml:lang="zh">RIPEMD-320算法</para>
///     <para xml:lang="en">A cryptographic hash function producing a 320-bit hash value</para>
///     <para xml:lang="zh">一种加密哈希函数,生成320位的哈希值</para>
/// </summary>
public static class RipeMD320
{
    /// <summary>
    ///     <para xml:lang="en">Gets the RIPEMD-320 hash of a string</para>
    ///     <para xml:lang="zh">获取字符串的RIPEMD-320哈希值</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input string</para>
    ///     <para xml:lang="zh">输入的字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The RIPEMD-320 hash as a byte array (40 bytes)</para>
    ///     <para xml:lang="zh">RIPEMD-320哈希值的字节数组(40字节)</para>
    /// </returns>
    public static byte[] Hash(string data)
    {
        ArgumentException.ThrowIfNullOrEmpty(data);
        var msg = Encoding.UTF8.GetBytes(data);
        return Hash(msg);
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the RIPEMD-320 hash of a byte array</para>
    ///     <para xml:lang="zh">获取字节数组的RIPEMD-320哈希值</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input byte array</para>
    ///     <para xml:lang="zh">输入的字节数组</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The RIPEMD-320 hash as a byte array (40 bytes)</para>
    ///     <para xml:lang="zh">RIPEMD-320哈希值的字节数组(40字节)</para>
    /// </returns>
    public static byte[] Hash(ReadOnlySpan<byte> data)
    {
        Span<byte> md = stackalloc byte[40];
        var digest = new RipeMD320Digest();
        digest.BlockUpdate(data);
        digest.DoFinal(md);
        return md.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Computes the RIPEMD-320 hash of a string and returns it as a hexadecimal string</para>
    ///     <para xml:lang="zh">计算字符串的RIPEMD-320哈希并返回十六进制字符串</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input string</para>
    ///     <para xml:lang="zh">输入的字符串</para>
    /// </param>
    /// <param name="upperCase">
    ///     <para xml:lang="en">Whether to return uppercase hexadecimal string (default: false)</para>
    ///     <para xml:lang="zh">是否返回大写的十六进制字符串(默认: false)</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The RIPEMD-320 hash as a hexadecimal string (80 characters)</para>
    ///     <para xml:lang="zh">RIPEMD-320哈希值的十六进制字符串(80个字符)</para>
    /// </returns>
    public static string HashToHex(string data, bool upperCase = false)
    {
        var hash = Hash(data);
        var hex = Convert.ToHexString(hash);
        return upperCase ? hex : hex.ToLowerInvariant();
    }

    /// <summary>
    ///     <para xml:lang="en">Computes the RIPEMD-320 hash of a byte array and returns it as a hexadecimal string</para>
    ///     <para xml:lang="zh">计算字节数组的RIPEMD-320哈希并返回十六进制字符串</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input byte array</para>
    ///     <para xml:lang="zh">输入的字节数组</para>
    /// </param>
    /// <param name="upperCase">
    ///     <para xml:lang="en">Whether to return uppercase hexadecimal string (default: false)</para>
    ///     <para xml:lang="zh">是否返回大写的十六进制字符串(默认: false)</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The RIPEMD-320 hash as a hexadecimal string (80 characters)</para>
    ///     <para xml:lang="zh">RIPEMD-320哈希值的十六进制字符串(80个字符)</para>
    /// </returns>
    public static string HashToHex(ReadOnlySpan<byte> data, bool upperCase = false)
    {
        var hash = Hash(data);
        var hex = Convert.ToHexString(hash);
        return upperCase ? hex : hex.ToLowerInvariant();
    }

    /// <summary>
    ///     <para xml:lang="en">Computes the RIPEMD-320 hash of a string and returns it as a Base64 string</para>
    ///     <para xml:lang="zh">计算字符串的RIPEMD-320哈希并返回Base64字符串</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input string</para>
    ///     <para xml:lang="zh">输入的字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The RIPEMD-320 hash as a Base64 string</para>
    ///     <para xml:lang="zh">RIPEMD-320哈希值的Base64字符串</para>
    /// </returns>
    public static string HashToBase64(string data)
    {
        var hash = Hash(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    ///     <para xml:lang="en">Computes the RIPEMD-320 hash of a byte array and returns it as a Base64 string</para>
    ///     <para xml:lang="zh">计算字节数组的RIPEMD-320哈希并返回Base64字符串</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input byte array</para>
    ///     <para xml:lang="zh">输入的字节数组</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The RIPEMD-320 hash as a Base64 string</para>
    ///     <para xml:lang="zh">RIPEMD-320哈希值的Base64字符串</para>
    /// </returns>
    public static string HashToBase64(ReadOnlySpan<byte> data)
    {
        var hash = Hash(data);
        return Convert.ToBase64String(hash);
    }
}
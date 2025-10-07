using System.Text;
using Org.BouncyCastle.Crypto.Digests;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">SM3 algorithm (decimal ASCII)</para>
///     <para xml:lang="zh">SM3算法(10进制的ASCII)</para>
///     <para xml:lang="en">An algorithm improved based on SHA-256</para>
///     <para xml:lang="zh">在SHA-256基础上改进实现的一种算法</para>
///     <para xml:lang="en">Comparable to international MD5 and SHA algorithms</para>
///     <para xml:lang="zh">对标国际MD5算法和SHA算法</para>
/// </summary>
public static class Sm3Signature
{
    /// <summary>
    ///     <para xml:lang="en">Gets the SM3 hash of a string (alias for Crypt)</para>
    ///     <para xml:lang="zh">获取字符串的SM3哈希(Crypt的别名)</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input string</para>
    ///     <para xml:lang="zh">输入的字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The SM3 hash as a byte array (32 bytes)</para>
    ///     <para xml:lang="zh">SM3哈希值的字节数组(32字节)</para>
    /// </returns>
    public static byte[] Hash(string data)
    {
        ArgumentException.ThrowIfNullOrEmpty(data);
        var msg = Encoding.UTF8.GetBytes(data);
        return Hash(msg);
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the SM3 hash of a byte array (alias for Crypt)</para>
    ///     <para xml:lang="zh">获取字节数组的SM3哈希(Crypt的别名)</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input byte array</para>
    ///     <para xml:lang="zh">输入的字节数组</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The SM3 hash as a byte array (32 bytes)</para>
    ///     <para xml:lang="zh">SM3哈希值的字节数组(32字节)</para>
    /// </returns>
    public static byte[] Hash(ReadOnlySpan<byte> data)
    {
        Span<byte> md = stackalloc byte[32];
        var sm3 = new SM3Digest();
        sm3.BlockUpdate(data);
        sm3.DoFinal(md);
        return md.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Computes the SM3 hash of a string and returns it as a hexadecimal string</para>
    ///     <para xml:lang="zh">计算字符串的SM3哈希并返回十六进制字符串</para>
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
    ///     <para xml:lang="en">The SM3 hash as a hexadecimal string (64 characters)</para>
    ///     <para xml:lang="zh">SM3哈希值的十六进制字符串(64个字符)</para>
    /// </returns>
    public static string HashToHex(string data, bool upperCase = false)
    {
        var hash = Hash(data);
        var hex = Convert.ToHexString(hash);
        return upperCase ? hex : hex.ToLowerInvariant();
    }

    /// <summary>
    ///     <para xml:lang="en">Computes the SM3 hash of a byte array and returns it as a hexadecimal string</para>
    ///     <para xml:lang="zh">计算字节数组的SM3哈希并返回十六进制字符串</para>
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
    ///     <para xml:lang="en">The SM3 hash as a hexadecimal string (64 characters)</para>
    ///     <para xml:lang="zh">SM3哈希值的十六进制字符串(64个字符)</para>
    /// </returns>
    public static string HashToHex(ReadOnlySpan<byte> data, bool upperCase = false)
    {
        var hash = Hash(data);
        var hex = Convert.ToHexString(hash);
        return upperCase ? hex : hex.ToLowerInvariant();
    }

    /// <summary>
    ///     <para xml:lang="en">Computes the SM3 hash of a string and returns it as a Base64 string</para>
    ///     <para xml:lang="zh">计算字符串的SM3哈希并返回Base64字符串</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input string</para>
    ///     <para xml:lang="zh">输入的字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The SM3 hash as a Base64 string</para>
    ///     <para xml:lang="zh">SM3哈希值的Base64字符串</para>
    /// </returns>
    public static string HashToBase64(string data)
    {
        var hash = Hash(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    ///     <para xml:lang="en">Computes the SM3 hash of a byte array and returns it as a Base64 string</para>
    ///     <para xml:lang="zh">计算字节数组的SM3哈希并返回Base64字符串</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input byte array</para>
    ///     <para xml:lang="zh">输入的字节数组</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The SM3 hash as a Base64 string</para>
    ///     <para xml:lang="zh">SM3哈希值的Base64字符串</para>
    /// </returns>
    public static string HashToBase64(ReadOnlySpan<byte> data)
    {
        var hash = Hash(data);
        return Convert.ToBase64String(hash);
    }
}
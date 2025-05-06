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
public static class Sm3Crypt
{
    /// <summary>
    ///     <para xml:lang="en">Gets the SM3 signature of a string</para>
    ///     <para xml:lang="zh">获取字符串的SM3签名</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input string</para>
    ///     <para xml:lang="zh">输入的字符串</para>
    /// </param>
    public static byte[] Signature(string data)
    {
        var msg = Encoding.UTF8.GetBytes(data);
        return Signature(msg);
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the SM3 signature of a byte array</para>
    ///     <para xml:lang="zh">获取字节数组的SM3签名</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The input byte array</para>
    ///     <para xml:lang="zh">输入的字节数组</para>
    /// </param>
    public static byte[] Signature(ReadOnlySpan<byte> data)
    {
        Span<byte> md = stackalloc byte[32];
        var sm3 = new SM3Digest();
        sm3.BlockUpdate(data);
        sm3.DoFinal(md);
        return md.ToArray();
    }
}
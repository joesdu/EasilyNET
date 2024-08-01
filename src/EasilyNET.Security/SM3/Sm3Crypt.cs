using System.Text;
using Org.BouncyCastle.Crypto.Digests;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Security;

/// <summary>
/// SM3算法(10进制的ASCII)
/// 在SHA-256基础上改进实现的一种算法
/// 对标国际MD5算法和SHA算法
/// </summary>
public static class Sm3Crypt
{
    /// <summary>
    /// 获取字符串的SM3签名
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static byte[] Signature(string data)
    {
        var msg = Encoding.UTF8.GetBytes(data);
        return Signature(msg);
    }

    /// <summary>
    /// 获取字节数组的SM3签名
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static byte[] Signature(byte[] data)
    {
        var md = new byte[32];
        var sm3 = new SM3Digest();
        sm3.BlockUpdate(data, 0, data.Length);
        sm3.DoFinal(md, 0);
        return md;
    }
}
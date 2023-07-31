using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Utilities.Encoders;
using System.Text;

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
    /// SM3加密,获取结果Base64字符串
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string ToSm3Base64(this string data) => Convert.ToBase64String(Hex.Decode(data.Crypt()));

    /// <summary>
    /// SM3加密,获取结果16进制字符串
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string ToSm3String(this string data) => new UTF8Encoding().GetString(data.Crypt());

    private static byte[] Crypt(this string data)
    {
        //加密
        var msg = Encoding.UTF8.GetBytes(data);
        var md = new byte[32];
        var sm3 = new SM3Digest();
        sm3.BlockUpdate(msg, 0, msg.Length);
        sm3.DoFinal(md, 0);
        return Hex.Encode(md);
    }
}
using System.Text;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// SHA,全称SecureHashAlgorithm,是一种数据加密算法,该算法的思想是接收一段明文,
/// 然后以一种不可逆的方式将它转换成一段(通常更小)密文,也可以简单的理解为取一串输入码(称为预映射或信息),
/// 并把它们转化为长度较短、位数固定的输出序列即散列值(也称为信息摘要或信息认证代码)的过程,SHA为不可逆加密方式
/// </summary>
public static class SHA
{
    /// <summary>
    /// SHA1
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static string SHA1(this string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
#if NETSTANDARD
        var sha = System.Security.Cryptography.SHA1.Create();
        var encrypt = sha.ComputeHash(bytes);
#else
        var encrypt = System.Security.Cryptography.SHA1.HashData(bytes);
#endif
        return Convert.ToBase64String(encrypt);
    }

    /// <summary>
    /// SHA256
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static string SHA256(this string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
#if NETSTANDARD
        var sha = System.Security.Cryptography.SHA256.Create();
        var encrypt = sha.ComputeHash(bytes);
#else
        var encrypt = System.Security.Cryptography.SHA256.HashData(bytes);
#endif
        return Convert.ToBase64String(encrypt);
    }

    /// <summary>
    /// SHA384
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static string SHA384(this string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
#if NETSTANDARD
        var sha = System.Security.Cryptography.SHA384.Create();
        var encrypt = sha.ComputeHash(bytes);
#else
        var encrypt = System.Security.Cryptography.SHA384.HashData(bytes);
#endif
        return Convert.ToBase64String(encrypt);
    }

    /// <summary>
    /// SHA512
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static string SHA512(this string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
#if NETSTANDARD
        var sha = System.Security.Cryptography.SHA512.Create();
        var encrypt = sha.ComputeHash(bytes);
#else
        var encrypt = System.Security.Cryptography.SHA512.HashData(bytes);
#endif
        return Convert.ToBase64String(encrypt);
    }
}
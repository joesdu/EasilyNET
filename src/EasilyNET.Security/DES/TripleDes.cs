using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// TripleDES加密解密(由于本库对密钥进行了hash算法处理.使用本库加密仅能用本库解密)
/// </summary>
public static class TripleDes
{
    /// <summary>
    /// 盐
    /// </summary>
    private const string slat = "HosW[A1]ew0sVtVzf[DfQ~x%hk2+ifMlg;)Wsf[9@Fh{_z$jNC";

    /// <summary>
    /// 处理key
    /// </summary>
    /// <param name="pwd">输入的密码</param>
    /// <returns></returns>
    private static (byte[] Key, byte[] IV) GetEesKey(string pwd)
    {
        Span<byte> keySpan = stackalloc byte[24];
        Span<byte> ivSpan = stackalloc byte[8];
        var hash1 = $"{pwd}-{slat}".To32MD5();
        var hash2 = $"{hash1}-{slat}".To32MD5();
        var hash3 = $"{hash2}-{slat}".To16MD5();
        Encoding.UTF8.GetBytes($"{hash1}{hash2}".To32MD5().AsSpan(0, 24), keySpan);
        Encoding.UTF8.GetBytes(hash3.AsSpan(0, 8), ivSpan);
        var key = keySpan.ToArray();
        var iv = ivSpan.ToArray();
        return (key, iv);
    }

    /// <summary>
    /// 使用给定密钥加密
    /// </summary>
    /// <param name="content">待加密数据</param>
    /// <param name="pwd">密钥</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns>加密后的数据</returns>
    public static byte[] Encrypt(ReadOnlySpan<byte> content, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        var (Key, IV) = GetEesKey(pwd);
        using var des = TripleDES.Create();
        des.Key = Key;
        des.IV = IV;
        des.Mode = mode;
        des.Padding = padding;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(content);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    /// <summary>
    /// 使用给定密钥解密数据
    /// </summary>
    /// <param name="secret">待解密数据</param>
    /// <param name="pwd">密钥</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns>解密后的数据</returns>
    public static byte[] Decrypt(ReadOnlySpan<byte> secret, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        var (Key, IV) = GetEesKey(pwd);
        using var des = TripleDES.Create();
        des.Key = Key;
        des.IV = IV;
        des.Mode = mode;
        des.Padding = padding;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
        cs.Write(secret);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }
}
using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// TripleDES加密解密
/// </summary>
public static class TripleDES
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
    private static Tuple<byte[], byte[]> GetEesKey(string pwd)
    {
        var hash1 = $"{pwd}-{slat}".To32MD5();
        var hash2 = $"{hash1}-{slat}".To32MD5();
        var hash3 = $"{hash2}-{slat}".To16MD5();
        var Key = Encoding.UTF8.GetBytes($"{hash1}{hash2}".To32MD5()[..24]);
        var IV = Encoding.UTF8.GetBytes(hash3[..8]);
        return new(Key, IV);
    }

    /// <summary>
    /// 使用给定密钥加密
    /// </summary>
    /// <param name="content">待加密的字符串</param>
    /// <param name="pwd">加密密钥</param>
    /// <returns>加密后的字符串</returns>
    public static string Encrypt(string content, string pwd)
    {
        var (Key, IV) = GetEesKey(pwd);
        var inputByteArray = Encoding.UTF8.GetBytes(content);
        var des = System.Security.Cryptography.TripleDES.Create();
        des.Key = Key;
        des.IV = IV;
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.PKCS7;
        var ms = new MemoryStream();
        var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(inputByteArray, 0, inputByteArray.Length);
        cs.FlushFinalBlock();
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// 使用给定密钥解密数据
    /// </summary>
    /// <param name="secret">待解密的字符串</param>
    /// <param name="pwd">解密密钥</param>
    /// <returns>解密后的字符串</returns>
    public static string Decrypt(string secret, string pwd)
    {
        var (Key, IV) = GetEesKey(pwd);
        var inputByteArray = Convert.FromBase64String(secret);
        var des = System.Security.Cryptography.TripleDES.Create();
        des.Key = Key;
        des.IV = IV;
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.PKCS7;
        var ms = new MemoryStream();
        var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
        cs.Write(inputByteArray, 0, inputByteArray.Length);
        cs.FlushFinalBlock();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
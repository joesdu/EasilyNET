using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// DES加密解密(由于本库对密钥进行了hash算法处理.使用本库加密仅能用本库解密)
/// </summary>
// ReSharper disable once UnusedType.Global
public static class DesCrypt
{
    /// <summary>
    /// 盐
    /// </summary>
    private const string slat = "Fo~@Ymf3w-!K+hYYoI^emXJeNt79pv@Sy,rpl0vXyIa-^jI{fU";

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
        var Key = Encoding.UTF8.GetBytes($"{hash1}{hash2}".To16MD5()[..8]);
        var IV = Encoding.UTF8.GetBytes(hash3[..8]);
        return new(Key, IV);
    }

    /// <summary>
    /// DES加密
    /// </summary>
    /// <param name="content">待加密数据</param>
    /// <param name="pwd">密钥</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns>加密后的数据</returns>
    public static byte[] Encrypt(byte[] content, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        var (Key, IV) = GetEesKey(pwd);
        var des = DES.Create();
        des.Key = Key;
        des.IV = IV;
        des.Mode = mode;
        des.Padding = padding;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(content, 0, content.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    /// <summary>
    /// DES解密字符串
    /// </summary>
    /// <param name="secret">待解密数据</param>
    /// <param name="pwd">密钥</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns>解密后的字符串</returns>
    public static byte[] Decrypt(byte[] secret, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        var (Key, IV) = GetEesKey(pwd);
        var des = DES.Create();
        des.Key = Key;
        des.IV = IV;
        des.Mode = mode;
        des.Padding = padding;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
        cs.Write(secret, 0, secret.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }
}
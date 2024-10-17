using System.Collections.Concurrent;
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
    /// 缓存密钥和IV
    /// </summary>
    private static readonly ConcurrentDictionary<string, (byte[] Key, byte[] IV)> KeyCache = new();

    /// <summary>
    /// 处理key
    /// </summary>
    /// <param name="pwd">输入的密码</param>
    /// <returns></returns>
    private static (byte[] Key, byte[] IV) GetEesKey(string pwd)
    {
        if (KeyCache.TryGetValue(pwd, out var cachedKey))
        {
            return cachedKey;
        }
        Span<byte> keySpan = stackalloc byte[8];
        Span<byte> ivSpan = stackalloc byte[8];
        var hash1 = $"{pwd}-{slat}".To32MD5();
        var hash2 = $"{hash1}-{slat}".To32MD5();
        var hash3 = $"{hash2}-{slat}".To16MD5();
        Encoding.UTF8.GetBytes($"{hash1}{hash2}".To16MD5().AsSpan(0, 8), keySpan);
        Encoding.UTF8.GetBytes(hash3.AsSpan(0, 8), ivSpan);
        var key = keySpan.ToArray();
        var iv = ivSpan.ToArray();
        KeyCache[pwd] = (key, iv);
        return (key, iv);
    }

    /// <summary>
    /// DES加密
    /// </summary>
    /// <param name="content">待加密数据</param>
    /// <param name="pwd">密钥</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns>加密后的数据</returns>
    public static byte[] Encrypt(ReadOnlySpan<byte> content, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        var (Key, IV) = GetEesKey(pwd);
        using var des = DES.Create();
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
    /// DES解密字符串
    /// </summary>
    /// <param name="secret">待解密数据</param>
    /// <param name="pwd">密钥</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns>解密后的字符串</returns>
    public static byte[] Decrypt(ReadOnlySpan<byte> secret, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        var (Key, IV) = GetEesKey(pwd);
        using var des = DES.Create();
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
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// AES加密解密(由于本库对密钥进行了hash算法处理.使用本库加密仅能用本库解密)
/// </summary>
public static class AesCrypt
{
    /// <summary>
    /// 盐
    /// </summary>
    private const string slat = "Q+OFqu]luparUP;Xn^_ktHX^FoWiK4C#;daRV(b1bbT_;HrrAL";

    /// <summary>
    /// 缓存密钥和IV
    /// </summary>
    private static readonly ConcurrentDictionary<string, (byte[] Key, byte[] IV)> KeyCache = new();

    /// <summary>
    /// 处理key
    /// </summary>
    /// <param name="pwd">输入的密码</param>
    /// <param name="model">Key和IV模式</param>
    /// <returns></returns>
    private static (byte[] Key, byte[] IV) GetAesKey(string pwd, AesKeyModel model = AesKeyModel.AES256)
    {
        var cacheKey = $"{pwd}-{model}";
        if (KeyCache.TryGetValue(cacheKey, out var cachedKey))
        {
            return cachedKey;
        }
        Span<byte> keySpan = stackalloc byte[32];
        Span<byte> ivSpan = stackalloc byte[16];
        var hash1 = $"{pwd}-{slat}".To32MD5();
        switch (model)
        {
            case AesKeyModel.AES256:
                var hash2 = $"{hash1}-{slat}".To32MD5();
                var hash3 = $"{hash2}-{slat}".To16MD5();
                Encoding.UTF8.GetBytes($"{hash1}{hash2}".To32MD5().AsSpan(0, 32), keySpan);
                Encoding.UTF8.GetBytes(hash3.AsSpan(0, 16), ivSpan);
                break;
            case AesKeyModel.AES128:
                var hash2_128 = $"{hash1}-{slat}".To16MD5();
                Encoding.UTF8.GetBytes(hash1.AsSpan(0, 16), keySpan);
                Encoding.UTF8.GetBytes(hash2_128.AsSpan(0, 16), ivSpan);
                break;
            default:
                throw new("不支持的类型");
        }
        var key = keySpan.ToArray();
        var iv = ivSpan.ToArray();
        KeyCache[cacheKey] = (key, iv);
        return (key, iv);
    }

    /// <summary>
    /// AES加密
    /// </summary>
    /// <param name="content">待加密数据</param>
    /// <param name="pwd">密钥</param>
    /// <param name="model">Aes密钥模式</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns></returns>
    public static byte[] Encrypt(ReadOnlySpan<byte> content, string pwd, AesKeyModel model, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        var (Key, IV) = GetAesKey(pwd, model);
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        aes.Mode = mode;
        aes.Padding = padding;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(content);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    /// <summary>
    /// AES解密
    /// </summary>
    /// <param name="secret">待解密数据</param>
    /// <param name="pwd">密钥</param>
    /// <param name="model">Aes密钥模式</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns></returns>
    public static byte[] Decrypt(ReadOnlySpan<byte> secret, string pwd, AesKeyModel model, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
    {
        var (Key, IV) = GetAesKey(pwd, model);
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        aes.Mode = mode;
        aes.Padding = padding;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
        cs.Write(secret);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }
}
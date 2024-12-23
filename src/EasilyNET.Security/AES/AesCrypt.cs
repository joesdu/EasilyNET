using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">
///     AES encryption and decryption (Due to the hash algorithm processing of the key in this library, encryption using this library
///     can only be decrypted by this library)
///     </para>
///     <para xml:lang="zh">AES加密解密(由于本库对密钥进行了hash算法处理.使用本库加密仅能用本库解密)</para>
/// </summary>
public static class AesCrypt
{
    /// <summary>
    ///     <para xml:lang="en">Salt</para>
    ///     <para xml:lang="zh">盐</para>
    /// </summary>
    private const string slat = "Q+OFqu]luparUP;Xn^_ktHX^FoWiK4C#;daRV(b1bbT_;HrrAL";

    /// <summary>
    ///     <para xml:lang="en">Cache for keys and IVs</para>
    ///     <para xml:lang="zh">缓存密钥和IV</para>
    /// </summary>
    private static readonly ConcurrentDictionary<string, (byte[] Key, byte[] IV)> KeyCache = new();

    /// <summary>
    ///     <para xml:lang="en">Processes the key</para>
    ///     <para xml:lang="zh">处理key</para>
    /// </summary>
    /// <param name="pwd">
    ///     <para xml:lang="en">The input password</para>
    ///     <para xml:lang="zh">输入的密码</para>
    /// </param>
    /// <param name="model">
    ///     <para xml:lang="en">Key and IV mode</para>
    ///     <para xml:lang="zh">Key和IV模式</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A tuple containing the key and IV</para>
    ///     <para xml:lang="zh">包含密钥和IV的元组</para>
    /// </returns>
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
    ///     <para xml:lang="en">AES encryption</para>
    ///     <para xml:lang="zh">AES加密</para>
    /// </summary>
    /// <param name="content">
    ///     <para xml:lang="en">Data to be encrypted</para>
    ///     <para xml:lang="zh">待加密数据</para>
    /// </param>
    /// <param name="pwd">
    ///     <para xml:lang="en">Key</para>
    ///     <para xml:lang="zh">密钥</para>
    /// </param>
    /// <param name="model">
    ///     <para xml:lang="en">AES key mode</para>
    ///     <para xml:lang="zh">Aes密钥模式</para>
    /// </param>
    /// <param name="mode">
    ///     <para xml:lang="en">Encryption mode</para>
    ///     <para xml:lang="zh">加密模式</para>
    /// </param>
    /// <param name="padding">
    ///     <para xml:lang="en">Padding mode</para>
    ///     <para xml:lang="zh">填充模式</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Encrypted data</para>
    ///     <para xml:lang="zh">加密数据</para>
    /// </returns>
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
    ///     <para xml:lang="en">AES decryption</para>
    ///     <para xml:lang="zh">AES解密</para>
    /// </summary>
    /// <param name="secret">
    ///     <para xml:lang="en">Data to be decrypted</para>
    ///     <para xml:lang="zh">待解密数据</para>
    /// </param>
    /// <param name="pwd">
    ///     <para xml:lang="en">Key</para>
    ///     <para xml:lang="zh">密钥</para>
    /// </param>
    /// <param name="model">
    ///     <para xml:lang="en">AES key mode</para>
    ///     <para xml:lang="zh">Aes密钥模式</para>
    /// </param>
    /// <param name="mode">
    ///     <para xml:lang="en">Encryption mode</para>
    ///     <para xml:lang="zh">加密模式</para>
    /// </param>
    /// <param name="padding">
    ///     <para xml:lang="en">Padding mode</para>
    ///     <para xml:lang="zh">填充模式</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Decrypted data</para>
    ///     <para xml:lang="zh">解密数据</para>
    /// </returns>
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
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">
///     DES encryption and decryption (Due to the hash algorithm processing of the key in this library, encryption using this library
///     can only be decrypted by this library)
///     </para>
///     <para xml:lang="zh">DES加密解密(由于本库对密钥进行了hash算法处理.使用本库加密仅能用本库解密)</para>
/// </summary>
// ReSharper disable once UnusedType.Global
public static class DesCrypt
{
    /// <summary>
    ///     <para xml:lang="en">Salt</para>
    ///     <para xml:lang="zh">盐</para>
    /// </summary>
    private const string slat = "Fo~@Ymf3w-!K+hYYoI^emXJeNt79pv@Sy,rpl0vXyIa-^jI{fU";

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
    ///     <para xml:lang="en">DES encryption</para>
    ///     <para xml:lang="zh">DES加密</para>
    /// </summary>
    /// <param name="content">
    ///     <para xml:lang="en">Data to be encrypted</para>
    ///     <para xml:lang="zh">待加密数据</para>
    /// </param>
    /// <param name="pwd">
    ///     <para xml:lang="en">Key</para>
    ///     <para xml:lang="zh">密钥</para>
    /// </param>
    /// <param name="mode">
    ///     <para xml:lang="en">Encryption mode</para>
    ///     <para xml:lang="zh">加密模式</para>
    /// </param>
    /// <param name="padding">
    ///     <para xml:lang="en">Padding mode</para>
    ///     <para xml:lang="zh">填充模式</para>
    /// </param>
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
    ///     <para xml:lang="en">DES decryption</para>
    ///     <para xml:lang="zh">DES解密字符串</para>
    /// </summary>
    /// <param name="secret">
    ///     <para xml:lang="en">Data to be decrypted</para>
    ///     <para xml:lang="zh">待解密数据</para>
    /// </param>
    /// <param name="pwd">
    ///     <para xml:lang="en">Key</para>
    ///     <para xml:lang="zh">密钥</para>
    /// </param>
    /// <param name="mode">
    ///     <para xml:lang="en">Encryption mode</para>
    ///     <para xml:lang="zh">加密模式</para>
    /// </param>
    /// <param name="padding">
    ///     <para xml:lang="en">Padding mode</para>
    ///     <para xml:lang="zh">填充模式</para>
    /// </param>
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
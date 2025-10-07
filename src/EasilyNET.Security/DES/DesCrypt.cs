using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">
///     DES encryption and decryption (Due to the hash algorithm processing of the key in this library, encryption using this library
///     can only be decrypted by this library)
///     </para>
///     <para xml:lang="zh">DES加密解密(由于本库对密钥进行了hash算法处理.使用本库加密仅能用本库解密)</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">
///     ⚠️ WARNING: DES is considered insecure and should not be used for new applications.
///     It is recommended to use AES256 instead. This is provided only for compatibility with legacy systems.
///     </para>
///     <para xml:lang="zh">
///     ⚠️ 警告: DES被认为是不安全的,不应用于新应用程序。
///     建议使用AES256代替。仅为与遗留系统兼容而提供。
///     </para>
/// </remarks>
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
    private static (byte[] Key, byte[] IV) GetDesKey(string pwd)
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
        var (Key, IV) = GetDesKey(pwd);
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
        var (Key, IV) = GetDesKey(pwd);
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

    #region String Encryption/Decryption Convenience Methods

    /// <summary>
    ///     <para xml:lang="en">Encrypt string and return Base64 encoded result</para>
    ///     <para xml:lang="zh">加密字符串并返回Base64编码结果</para>
    /// </summary>
    /// <param name="content">
    ///     <para xml:lang="en">String to be encrypted</para>
    ///     <para xml:lang="zh">待加密字符串</para>
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
    /// <param name="encoding">
    ///     <para xml:lang="en">Character encoding, default: UTF8</para>
    ///     <para xml:lang="zh">字符编码,默认:UTF8</para>
    /// </param>
    public static string EncryptToBase64(string content, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(content);
        var encrypted = Encrypt(bytes, pwd, mode, padding);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt Base64 encoded string</para>
    ///     <para xml:lang="zh">解密Base64编码的字符串</para>
    /// </summary>
    /// <param name="base64Content">
    ///     <para xml:lang="en">Base64 encoded encrypted string</para>
    ///     <para xml:lang="zh">Base64编码的加密字符串</para>
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
    /// <param name="encoding">
    ///     <para xml:lang="en">Character encoding, default: UTF8</para>
    ///     <para xml:lang="zh">字符编码,默认:UTF8</para>
    /// </param>
    public static string DecryptFromBase64(string base64Content, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = Convert.FromBase64String(base64Content);
        var decrypted = Decrypt(bytes, pwd, mode, padding);
        return encoding.GetString(decrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt string and return hexadecimal result</para>
    ///     <para xml:lang="zh">加密字符串并返回十六进制结果</para>
    /// </summary>
    /// <param name="content">
    ///     <para xml:lang="en">String to be encrypted</para>
    ///     <para xml:lang="zh">待加密字符串</para>
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
    /// <param name="encoding">
    ///     <para xml:lang="en">Character encoding, default: UTF8</para>
    ///     <para xml:lang="zh">字符编码,默认:UTF8</para>
    /// </param>
    public static string EncryptToHex(string content, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(content);
        var encrypted = Encrypt(bytes, pwd, mode, padding);
        return Convert.ToHexString(encrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt hexadecimal encoded string</para>
    ///     <para xml:lang="zh">解密十六进制编码的字符串</para>
    /// </summary>
    /// <param name="hexContent">
    ///     <para xml:lang="en">Hexadecimal encoded encrypted string</para>
    ///     <para xml:lang="zh">十六进制编码的加密字符串</para>
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
    /// <param name="encoding">
    ///     <para xml:lang="en">Character encoding, default: UTF8</para>
    ///     <para xml:lang="zh">字符编码,默认:UTF8</para>
    /// </param>
    public static string DecryptFromHex(string hexContent, string pwd, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = Convert.FromHexString(hexContent);
        var decrypted = Decrypt(bytes, pwd, mode, padding);
        return encoding.GetString(decrypted);
    }

    #endregion
}
using System.Collections.Concurrent;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">RC4 encryption and decryption</para>
///     <para xml:lang="zh">RC4 加密解密</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">
///     ⚠️ CRITICAL WARNING: RC4 is considered INSECURE and BROKEN. DO NOT use for new applications!
///     RC4 has serious cryptographic weaknesses and is deprecated by major security standards.
///     This is provided ONLY for compatibility with legacy systems that still require RC4.
///     STRONGLY RECOMMENDED: Use AES256 for all new implementations.
///     </para>
///     <para xml:lang="zh">
///     ⚠️ 严重警告: RC4被认为是不安全和已破解的。请勿用于新应用程序!
///     RC4存在严重的密码学弱点,已被主要安全标准弃用。
///     仅为仍需要RC4的遗留系统提供兼容性。
///     强烈建议: 所有新实现请使用AES256。
///     </para>
/// </remarks>
public static class Rc4Crypt
{
    /// <summary>
    ///     <para xml:lang="en">Cache for derived keys</para>
    ///     <para xml:lang="zh">派生密钥缓存</para>
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte[]> KeyCache = new();

    /// <summary>
    ///     <para xml:lang="en">Derive key from password</para>
    ///     <para xml:lang="zh">从密码派生密钥</para>
    /// </summary>
    /// <param name="pwd">
    ///     <para xml:lang="en">Password</para>
    ///     <para xml:lang="zh">密码</para>
    /// </param>
    private static byte[] GetRc4Key(string pwd)
    {
        if (KeyCache.TryGetValue(pwd, out var cachedKey))
        {
            return cachedKey;
        }
        var key = Encoding.UTF8.GetBytes(pwd);
        KeyCache[pwd] = key;
        return key;
    }

    #region Low-level byte array methods

    /// <summary>
    ///     <para xml:lang="en">RC4 decryption (raw bytes)</para>
    ///     <para xml:lang="zh">RC4解密(原始字节)</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">Data to be decrypted</para>
    ///     <para xml:lang="zh">待解密数据</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Key (raw bytes)</para>
    ///     <para xml:lang="zh">密钥(原始字节)</para>
    /// </param>
    public static byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key) => Encrypt(data, key);

    /// <summary>
    ///     <para xml:lang="en">RC4 encryption (raw bytes)</para>
    ///     <para xml:lang="zh">RC4加密(原始字节)</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">Data to be encrypted</para>
    ///     <para xml:lang="zh">待加密数据</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Key (raw bytes)</para>
    ///     <para xml:lang="zh">密钥(原始字节)</para>
    /// </param>
    public static byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        Span<byte> s = stackalloc byte[256];
        EncryptInit(key, s);
        var i = 0;
        var j = 0;
        var result = new byte[data.Length];
        for (var k = 0; k < data.Length; k++)
        {
            i = (i + 1) & 255;
            j = (j + s[i]) & 255;
            Swap(s, i, j);
            result[k] = (byte)(data[k] ^ s[(s[i] + s[j]) & 255]);
        }
        return result;
    }

    #endregion

    #region Password-based methods

    /// <summary>
    ///     <para xml:lang="en">RC4 encryption with password</para>
    ///     <para xml:lang="zh">使用密码进行RC4加密</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">Data to be encrypted</para>
    ///     <para xml:lang="zh">待加密数据</para>
    /// </param>
    /// <param name="pwd">
    ///     <para xml:lang="en">Password</para>
    ///     <para xml:lang="zh">密码</para>
    /// </param>
    public static byte[] EncryptWithPassword(ReadOnlySpan<byte> data, string pwd)
    {
        var key = GetRc4Key(pwd);
        return Encrypt(data, key);
    }

    /// <summary>
    ///     <para xml:lang="en">RC4 decryption with password</para>
    ///     <para xml:lang="zh">使用密码进行RC4解密</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">Data to be decrypted</para>
    ///     <para xml:lang="zh">待解密数据</para>
    /// </param>
    /// <param name="pwd">
    ///     <para xml:lang="en">Password</para>
    ///     <para xml:lang="zh">密码</para>
    /// </param>
    public static byte[] DecryptWithPassword(ReadOnlySpan<byte> data, string pwd)
    {
        var key = GetRc4Key(pwd);
        return Decrypt(data, key);
    }

    #endregion

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
    ///     <para xml:lang="en">Password</para>
    ///     <para xml:lang="zh">密码</para>
    /// </param>
    /// <param name="encoding">
    ///     <para xml:lang="en">Character encoding, default: UTF8</para>
    ///     <para xml:lang="zh">字符编码,默认:UTF8</para>
    /// </param>
    public static string EncryptToBase64(string content, string pwd, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(content);
        var encrypted = EncryptWithPassword(bytes, pwd);
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
    ///     <para xml:lang="en">Password</para>
    ///     <para xml:lang="zh">密码</para>
    /// </param>
    /// <param name="encoding">
    ///     <para xml:lang="en">Character encoding, default: UTF8</para>
    ///     <para xml:lang="zh">字符编码,默认:UTF8</para>
    /// </param>
    public static string DecryptFromBase64(string base64Content, string pwd, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = Convert.FromBase64String(base64Content);
        var decrypted = DecryptWithPassword(bytes, pwd);
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
    ///     <para xml:lang="en">Password</para>
    ///     <para xml:lang="zh">密码</para>
    /// </param>
    /// <param name="encoding">
    ///     <para xml:lang="en">Character encoding, default: UTF8</para>
    ///     <para xml:lang="zh">字符编码,默认:UTF8</para>
    /// </param>
    public static string EncryptToHex(string content, string pwd, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(content);
        var encrypted = EncryptWithPassword(bytes, pwd);
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
    ///     <para xml:lang="en">Password</para>
    ///     <para xml:lang="zh">密码</para>
    /// </param>
    /// <param name="encoding">
    ///     <para xml:lang="en">Character encoding, default: UTF8</para>
    ///     <para xml:lang="zh">字符编码,默认:UTF8</para>
    /// </param>
    public static string DecryptFromHex(string hexContent, string pwd, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = Convert.FromHexString(hexContent);
        var decrypted = DecryptWithPassword(bytes, pwd);
        return encoding.GetString(decrypted);
    }

    #endregion

    #region Private Helper Methods

    private static void EncryptInit(ReadOnlySpan<byte> key, Span<byte> s)
    {
        for (var i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }
        for (int i = 0, j = 0; i < 256; i++)
        {
            j = (j + key[i % key.Length] + s[i]) & 255;
            Swap(s, i, j);
        }
    }

    private static void Swap(Span<byte> s, int i, int j) => (s[i], s[j]) = (s[j], s[i]);

    #endregion
}
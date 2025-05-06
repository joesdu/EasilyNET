using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">
///     TripleDES encryption and decryption (Due to the hash algorithm processing of the key in this library, encryption using this
///     library can only be decrypted by this library)
///     </para>
///     <para xml:lang="zh">TripleDES加密解密(由于本库对密钥进行了hash算法处理.使用本库加密仅能用本库解密)</para>
/// </summary>
public static class TripleDes
{
    /// <summary>
    ///     <para xml:lang="en">Salt</para>
    ///     <para xml:lang="zh">盐</para>
    /// </summary>
    private const string slat = "HosW[A1]ew0sVtVzf[DfQ~x%hk2+ifMlg;)Wsf[9@Fh{_z$jNC";

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
    ///     <para xml:lang="en">Encrypts data using the given key</para>
    ///     <para xml:lang="zh">使用给定密钥加密</para>
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
    ///     <para xml:lang="en">Decrypts data using the given key</para>
    ///     <para xml:lang="zh">使用给定密钥解密数据</para>
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
using System.Text;
using Org.BouncyCastle.Utilities.Encoders;

// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">SM4 Encryption</para>
///     <para xml:lang="zh">SM4加密</para>
/// </summary>
public static class Sm4Crypt
{
    /// <summary>
    ///     <para xml:lang="en">Encrypt using ECB mode</para>
    ///     <para xml:lang="zh">加密ECB模式</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key</para>
    ///     <para xml:lang="zh">密钥</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key is in hexadecimal</para>
    ///     <para xml:lang="zh">密钥是否是十六进制</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text in binary format</para>
    ///     <para xml:lang="zh">二进制格式加密的内容</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Returns the ciphertext in binary format</para>
    ///     <para xml:lang="zh">返回二进制格式密文</para>
    /// </returns>
    public static byte[] EncryptECB(string secretKey, bool hexString, ReadOnlySpan<byte> plainText)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Encrypt
        };
        var keyBytes = hexString ? Hex.Decode(secretKey) : Encoding.UTF8.GetBytes(secretKey);
        var sm4 = new Sm4();
        sm4.SetKeyEnc(ctx, keyBytes);
        return sm4.ECB(ctx, plainText);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt using ECB mode</para>
    ///     <para xml:lang="zh">解密ECB模式</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key</para>
    ///     <para xml:lang="zh">密钥</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key is in hexadecimal</para>
    ///     <para xml:lang="zh">密钥是否是十六进制</para>
    /// </param>
    /// <param name="cipherBytes">
    ///     <para xml:lang="en">Ciphertext in binary format</para>
    ///     <para xml:lang="zh">二进制格式密文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Returns the plaintext in binary format</para>
    ///     <para xml:lang="zh">返回二进制格式明文</para>
    /// </returns>
    public static byte[] DecryptECB(string secretKey, bool hexString, ReadOnlySpan<byte> cipherBytes)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Decrypt
        };
        var keyBytes = hexString ? Hex.Decode(secretKey) : Encoding.UTF8.GetBytes(secretKey);
        var sm4 = new Sm4();
        sm4.SetKeyDec(ctx, keyBytes);
        return sm4.ECB(ctx, cipherBytes);
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt using CBC mode</para>
    ///     <para xml:lang="zh">加密CBC模式</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key</para>
    ///     <para xml:lang="zh">密钥</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key and IV are in hexadecimal</para>
    ///     <para xml:lang="zh">密钥和IV是否是十六进制</para>
    /// </param>
    /// <param name="iv">
    ///     <para xml:lang="en">Initialization vector</para>
    ///     <para xml:lang="zh">初始化向量</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text in binary format</para>
    ///     <para xml:lang="zh">二进制格式明文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Returns the ciphertext in binary format</para>
    ///     <para xml:lang="zh">返回二进制密文数组</para>
    /// </returns>
    public static byte[] EncryptCBC(string secretKey, bool hexString, string iv, ReadOnlySpan<byte> plainText)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Encrypt
        };
        var keyBytes = hexString ? Hex.Decode(secretKey) : Encoding.UTF8.GetBytes(secretKey);
        var ivBytes = hexString ? Hex.Decode(iv) : Encoding.UTF8.GetBytes(iv);
        var sm4 = new Sm4();
        sm4.SetKeyEnc(ctx, keyBytes);
        return sm4.CBC(ctx, ivBytes, plainText);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt using CBC mode</para>
    ///     <para xml:lang="zh">解密CBC模式</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key</para>
    ///     <para xml:lang="zh">密钥</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key and IV are in hexadecimal</para>
    ///     <para xml:lang="zh">密钥和IV是否是十六进制</para>
    /// </param>
    /// <param name="iv">
    ///     <para xml:lang="en">Initialization vector</para>
    ///     <para xml:lang="zh">初始化向量</para>
    /// </param>
    /// <param name="cipherText">
    ///     <para xml:lang="en">Ciphertext in binary format</para>
    ///     <para xml:lang="zh">二进制格式密文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Returns the plaintext in binary format</para>
    ///     <para xml:lang="zh">返回二进制格式明文</para>
    /// </returns>
    public static byte[] DecryptCBC(string secretKey, bool hexString, string iv, ReadOnlySpan<byte> cipherText)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Decrypt
        };
        var keyBytes = hexString ? Hex.Decode(secretKey) : Encoding.UTF8.GetBytes(secretKey);
        var ivBytes = hexString ? Hex.Decode(iv) : Encoding.UTF8.GetBytes(iv);
        var sm4 = new Sm4();
        sm4.SetKeyDec(ctx, keyBytes);
        return sm4.CBC(ctx, ivBytes, cipherText);
    }
}
using EasilyNET.Core.Misc;
using Org.BouncyCastle.Utilities.Encoders;
using System.Text;

namespace EasilyNET.Security;

/// <summary>
/// SM4加密
/// </summary>
public static class Sm4Crypt
{
    /// <summary>
    /// Base64字符串转16进制字符串
    /// </summary>
    /// <param name="base64"></param>
    /// <returns></returns>
    public static string Base64ToHex16(this string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        return bytes.ToHexString();
    }

    /// <summary>
    /// 将Hex16转化成Base64字符串
    /// </summary>
    /// <param name="hex16"></param>
    /// <returns></returns>
    public static string Hex16ToBase64(this string hex16)
    {
        var bytes = Convert.FromHexString(hex16);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 加密ECB模式
    /// </summary>
    /// <param name="secretKey">密钥</param>
    /// <param name="hexString">密钥是否是十六进制</param>
    /// <param name="plainText">明文</param>
    /// <returns>返回Base64密文</returns>
    public static string EncryptECB(string secretKey, bool hexString, string plainText)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = Sm4.SM4_ENCRYPT
        };
        var keyBytes = hexString ? Hex.Decode(secretKey) : Encoding.Default.GetBytes(secretKey);
        var sm4 = new Sm4();
        sm4.Sm4SetKeyEnc(ctx, keyBytes);
        var contentBytes = Encoding.Default.GetBytes(plainText);
        var encrypted = sm4.Sm4CryptECB(ctx, contentBytes);
        var cipherText = Convert.ToBase64String(encrypted);
        return cipherText;
    }

    /// <summary>
    /// 解密ECB模式
    /// </summary>
    /// <param name="secretKey">密钥</param>
    /// <param name="hexString">密钥是否是十六进制</param>
    /// <param name="cipherText">Base64密文</param>
    /// <returns>返回明文</returns>
    public static string DecryptECB(string secretKey, bool hexString, string cipherText)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = Sm4.SM4_DECRYPT
        };
        var keyBytes = hexString ? Hex.Decode(secretKey) : Encoding.Default.GetBytes(secretKey);
        var sm4 = new Sm4();
        sm4.Sm4SetKeyDec(ctx, keyBytes);
        var contentBytes = Convert.FromBase64String(cipherText);
        var decrypted = sm4.Sm4CryptECB(ctx, contentBytes);
        return Encoding.Default.GetString(decrypted);
    }

    /// <summary>
    /// 加密CBC模式
    /// </summary>
    /// <param name="secretKey">密钥</param>
    /// <param name="hexString">密钥和IV是否是十六进制</param>
    /// <param name="iv"></param>
    /// <param name="plainText">明文</param>
    /// <returns>返回16进制密文</returns>
    public static string EncryptCBC(string secretKey, bool hexString, string iv, string plainText)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = Sm4.SM4_ENCRYPT
        };
        byte[] keyBytes;
        byte[] ivBytes;
        if (hexString)
        {
            keyBytes = Hex.Decode(secretKey);
            ivBytes = Hex.Decode(iv);
        }
        else
        {
            keyBytes = Encoding.Default.GetBytes(secretKey);
            ivBytes = Encoding.Default.GetBytes(iv);
        }
        var sm4 = new Sm4();
        sm4.Sm4SetKeyEnc(ctx, keyBytes);
        var encrypted = sm4.Sm4CryptCBC(ctx, ivBytes, Encoding.Default.GetBytes(plainText));
        var cipherText = Encoding.Default.GetString(Hex.Encode(encrypted));
        return cipherText;
    }

    /// <summary>
    /// 解密CBC模式
    /// </summary>
    /// <param name="secretKey">16进制密钥</param>
    /// <param name="hexString">密钥和IV是否是十六进制</param>
    /// <param name="iv"></param>
    /// <param name="cipherText">密文</param>
    /// <returns>返回明文</returns>
    public static string DecryptCBC(string secretKey, bool hexString, string iv, string cipherText)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = Sm4.SM4_DECRYPT
        };
        byte[] keyBytes;
        byte[] ivBytes;
        if (hexString)
        {
            keyBytes = Hex.Decode(secretKey);
            ivBytes = Hex.Decode(iv);
        }
        else
        {
            keyBytes = Encoding.Default.GetBytes(secretKey);
            ivBytes = Encoding.Default.GetBytes(iv);
        }
        var sm4 = new Sm4();
        sm4.Sm4SetKeyDec(ctx, keyBytes);
        var decrypted = sm4.Sm4CryptCBC(ctx, ivBytes, Hex.Decode(cipherText));
        return Encoding.Default.GetString(decrypted);
    }
}
using System.Text;
using Org.BouncyCastle.Utilities.Encoders;

// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// SM4加密
/// </summary>
public static class Sm4Crypt
{
    /// <summary>
    /// 加密ECB模式
    /// </summary>
    /// <param name="secretKey">密钥</param>
    /// <param name="hexString">密钥是否是十六进制</param>
    /// <param name="plainText">二进制格式加密的内容</param>
    /// <returns>返回二进制格式密文</returns>
    public static byte[] EncryptECB(string secretKey, bool hexString, byte[] plainText)
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
    /// 解密ECB模式
    /// </summary>
    /// <param name="secretKey">密钥</param>
    /// <param name="hexString">密钥是否是十六进制</param>
    /// <param name="cipherBytes">二进制格式密文</param>
    /// <returns>二进制格式明文</returns>
    public static byte[] DecryptECB(string secretKey, bool hexString, byte[] cipherBytes)
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
    /// 加密CBC模式
    /// </summary>
    /// <param name="secretKey">密钥</param>
    /// <param name="hexString">密钥和IV是否是十六进制</param>
    /// <param name="iv"></param>
    /// <param name="plainText">二进制格式明文</param>
    /// <returns>返回二进制密文数组</returns>
    public static byte[] EncryptCBC(string secretKey, bool hexString, string iv, byte[] plainText)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Encrypt
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
            keyBytes = Encoding.UTF8.GetBytes(secretKey);
            ivBytes = Encoding.UTF8.GetBytes(iv);
        }
        var sm4 = new Sm4();
        sm4.SetKeyEnc(ctx, keyBytes);
        return sm4.CBC(ctx, ivBytes, plainText);
    }

    /// <summary>
    /// 解密CBC模式
    /// </summary>
    /// <param name="secretKey">16进制密钥</param>
    /// <param name="hexString">密钥和IV是否是十六进制</param>
    /// <param name="iv"></param>
    /// <param name="cipherText">二进制格式密文</param>
    /// <returns>返回二进制格式明文</returns>
    public static byte[] DecryptCBC(string secretKey, bool hexString, string iv, byte[] cipherText)
    {
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Decrypt
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
            keyBytes = Encoding.UTF8.GetBytes(secretKey);
            ivBytes = Encoding.UTF8.GetBytes(iv);
        }
        var sm4 = new Sm4();
        sm4.SetKeyDec(ctx, keyBytes);
        return sm4.CBC(ctx, ivBytes, cipherText);
    }
}
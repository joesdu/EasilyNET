using EasilyNET.Core.Misc;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// AES加密解密(使用本库加密仅能用本库解密)
/// </summary>
public static class AesCrypt
{
    /// <summary>
    /// 盐
    /// </summary>
    private const string slat = "Q+OFqu]luparUP;Xn^_ktHX^FoWiK4C#;daRV(b1bbT_;HrrAL";

    /// <summary>
    /// 处理key
    /// </summary>
    /// <param name="pwd">输入的密码</param>
    /// <param name="model">Key和IV模式</param>
    /// <returns></returns>
    private static Tuple<byte[], byte[]> GetAesKey(string pwd, AESModel model = AESModel.AES256)
    {
        var hash1 = $"{pwd}-{slat}".To32MD5();
        switch (model)
        {
            case AESModel.AES256:
            {
                var hash2 = $"{hash1}-{slat}".To32MD5();
                var hash3 = $"{hash2}-{slat}".To16MD5();
                var Key = Encoding.UTF8.GetBytes($"{hash1}{hash2}".To32MD5());
                var IV = Encoding.UTF8.GetBytes(hash3);
                return new(Key, IV);
            }
            case AESModel.AES128:
            {
                var hash2 = $"{hash1}-{slat}".To16MD5();
                var Key = Encoding.UTF8.GetBytes(hash1);
                var IV = Encoding.UTF8.GetBytes(hash2);
                return new(Key, IV);
            }
            default: throw new("不支持的类型");
        }
    }

    /// <summary>
    /// AES加密
    /// </summary>
    /// <param name="content">需要加密的内容</param>
    /// <param name="pwd">密钥</param>
    /// <param name="model">加密模式</param>
    /// <returns></returns>
    public static byte[] Encrypt(byte[] content, string pwd, AESModel model)
    {
        var (Key, IV) = GetAesKey(pwd, model);
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        var cTransform = aes.CreateEncryptor();
        return cTransform.TransformFinalBlock(content, 0, content.Length);
    }

    /// <summary>
    /// AES解密
    /// </summary>
    /// <param name="secret"></param>
    /// <param name="pwd"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    public static byte[] Decrypt(byte[] secret, string pwd, AESModel model)
    {
        var (Key, IV) = GetAesKey(pwd, model);
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        var cTransform = aes.CreateDecryptor();
        return cTransform.TransformFinalBlock(secret, 0, secret.Length);
    }
}
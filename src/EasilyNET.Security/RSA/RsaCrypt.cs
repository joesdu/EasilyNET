using System.Security.Cryptography;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Security;

/// <summary>
/// RSA算法
/// </summary>
public static class RsaCrypt
{
    /// <summary>
    /// 创建RSA密钥对
    /// </summary>
    /// <param name="keySize">密钥的大小,必须384位到16384位,增量为8</param>
    /// <returns></returns>
    public static RsaSecretKey GenerateKey(int keySize)
    {
        if (keySize is > 16384 or < 384 && keySize % 8 is not 0)
        {
            throw new ArgumentException("密钥的大小,必须384位到16384位,增量为8", nameof(keySize));
        }
        var rsa = new RSACryptoServiceProvider(keySize)
        {
            KeySize = keySize,
            PersistKeyInCsp = false
        };
        return new()
        {
            PrivateKey = rsa.ToXmlString(true),
            PublicKey = rsa.ToXmlString(false)
        };
    }

    /// <summary>
    /// 创建RSA密钥对
    /// </summary>
    /// <param name="keySize">密钥的大小,提供常用长度枚举</param>
    /// <returns></returns>
    public static RsaSecretKey GenerateKey(ERsaKeyLength keySize) => GenerateKey((int)keySize);

    /// <summary>
    /// 使用RSA的加密byte[](该方法存在长度限制)
    /// </summary>
    /// <param name="xmlPublicKey">当前RSA对象的密匙XML字符串(不包括专用参数)--公钥</param>
    /// <param name="content">需要进行加密的字节数组</param>
    /// <returns>加密后的数据</returns>
    public static byte[] Encrypt(string xmlPublicKey, byte[] content)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPublicKey);
        return rsa.Encrypt(content, false);
    }

    /// <summary>
    /// RSA解密byte[](该方法存在长度限制)
    /// </summary>
    /// <param name="xmlPrivateKey">当前RSA对象的密匙XML字符串(包括专用参数)--私钥</param>
    /// <param name="secret">需要进行解密的字节数组</param>
    /// <returns>解密后的字符串</returns>
    public static byte[] Decrypt(string xmlPrivateKey, byte[] secret)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPrivateKey);
        return rsa.Decrypt(secret, false);
    }

    /// <summary>
    /// RSA加密 不限长度的加密版本
    /// </summary>
    /// <param name="xmlPublicKey">公匙</param>
    /// <param name="content">需要进行加密的数据</param>
    /// <param name="secret">加密后的数据</param>
    public static void Encrypt(string xmlPublicKey, byte[] content, out byte[] secret)
    {
        if (content.Length is 0) throw new("加密字符串不能为空.");
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(xmlPublicKey, nameof(xmlPublicKey));
#else
        if (string.IsNullOrWhiteSpace(xmlPublicKey)) throw new ArgumentException("错误的公匙");
#endif
        using var rsaProvider = new RSACryptoServiceProvider();
        rsaProvider.FromXmlString(xmlPublicKey);          //载入公钥
        var bufferSize = (rsaProvider.KeySize >> 3) - 11; //单块最大长度
        var buffer = new byte[bufferSize];
        using MemoryStream ms = new(content), os = new();
        while (true)
        {
            //分段加密
            var readSize = ms.Read(buffer, 0, bufferSize);
            if (readSize <= 0) break;
            var temp = new byte[readSize];
            Array.Copy(buffer, 0, temp, 0, readSize);
            var encryptedBytes = rsaProvider.Encrypt(temp, false);
            os.Write(encryptedBytes, 0, encryptedBytes.Length);
        }
        secret = os.ToArray(); //转化为字节流方便传输
    }

    /// <summary>
    /// RSA解密 不限长度的解密版本
    /// </summary>
    /// <param name="xmlPrivateKey">私匙</param>
    /// <param name="secret">需要进行解密的数据</param>
    /// <param name="context">解密后的数据</param>
    public static void Decrypt(string xmlPrivateKey, byte[] secret, out byte[] context)
    {
        if (secret.Length is 0) throw new("解密字符串不能为空.");
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(xmlPrivateKey, nameof(xmlPrivateKey));
#else
        if (string.IsNullOrWhiteSpace(xmlPrivateKey)) throw new ArgumentException("错误的私匙");
#endif
        using var rsaProvider = new RSACryptoServiceProvider();
        rsaProvider.FromXmlString(xmlPrivateKey);
        var bufferSize = rsaProvider.KeySize >> 3;
        var buffer = new byte[bufferSize];
        using MemoryStream ms = new(secret), os = new();
        while (true)
        {
            var readSize = ms.Read(buffer, 0, bufferSize);
            if (readSize <= 0) break;
            var temp = new byte[readSize];
            Array.Copy(buffer, 0, temp, 0, readSize);
            var rawBytes = rsaProvider.Decrypt(temp, false);
            os.Write(rawBytes, 0, rawBytes.Length);
        }
        context = os.ToArray();
    }

    /// <summary>
    /// 从文件中取得SHA256描述信息
    /// </summary>
    /// <param name="objFile"></param>
    /// <returns></returns>
    public static string GetFileSHA256(FileStream objFile)
    {
        ArgumentNullException.ThrowIfNull(objFile, nameof(objFile));
        using var stream = new MemoryStream();
        objFile.CopyTo(stream);
        var bytes = stream.ToArray();
        var array = SHA256.HashData(bytes);
        objFile.Close();
        return Encoding.UTF8.GetString(array);
    }

    #region RSA签名与签名验证

    /// <summary>
    /// RSA签名
    /// </summary>
    /// <param name="xmlPrivateKey">当前RSA对象的密匙XML字符串(包括专用参数)--私钥</param>
    /// <param name="context">需要签名的数据</param>
    /// <returns>签名数据</returns>
    public static byte[] Signature(string xmlPrivateKey, byte[] context)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPrivateKey);
        var RSAFormatter = new RSAPKCS1SignatureFormatter(rsa);
        //设置签名的算法为SHA256
        RSAFormatter.SetHashAlgorithm("SHA256");
        //执行签名 
        return RSAFormatter.CreateSignature(context);
    }

    /// <summary>
    /// RSA 签名验证
    /// </summary>
    /// <param name="xmlPublicKey">当前RSA对象的密匙XML字符串(不包括专用参数)--公钥</param>
    /// <param name="secret">用RSA签名的数据[俗称:Hash描述字符串,即:MD5或者SHA256这种.本库使用SHA256]</param>
    /// <param name="signature">要为该数据验证的已签名数据</param>
    /// <returns>如果 Verification 与使用指定的哈希算法和密钥在 signature 上计算出的签名匹配,则为 <see langword="true" />;否则为 <see langword="false" />.</returns>
    public static bool Verification(string xmlPublicKey, byte[] secret, byte[] signature)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPublicKey);
        var formatter = new RSAPKCS1SignatureDeformatter(rsa);
        //指定解密的时候HASH算法为SHA256
        formatter.SetHashAlgorithm("SHA256");
        return formatter.VerifySignature(secret, signature);
    }

    #endregion
}
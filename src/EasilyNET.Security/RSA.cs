using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Security;

/// <summary>
/// RSA算法
/// </summary>
public static class RSA
{
    /// <summary>
    /// 创建RSA密钥对
    /// </summary>
    /// <param name="keySize">密钥的大小,必须384位到16384位,增量为8</param>
    /// <returns></returns>
    public static RSASecretKey GenerateKey(int keySize)
    {
        using var rsa = new RSACryptoServiceProvider(keySize)
        {
            KeySize = 0,
            PersistKeyInCsp = false
        };
        return new()
        {
            PrivateKey = rsa.ToXmlString(true),
            PublicKey = rsa.ToXmlString(false)
        };
    }

    /// <summary>
    /// 使用RSA的加密byte[](该方法存在长度限制)
    /// </summary>
    /// <param name="xmlPublicKey">当前RSA对象的密匙XML字符串(不包括专用参数)--公钥</param>
    /// <param name="content">需要进行加密的字节数组</param>
    /// <returns>加密后的Base64字符串</returns>
    public static string Encrypt(string xmlPublicKey, string content)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPublicKey);
        var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(content), false);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// RSA解密byte[](该方法存在长度限制)
    /// </summary>
    /// <param name="xmlPrivateKey">当前RSA对象的密匙XML字符串(包括专用参数)--私钥</param>
    /// <param name="secret">需要进行解密的字节数组</param>
    /// <returns>解密后的字符串</returns>
    public static string Decrypt(string xmlPrivateKey, string secret)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPrivateKey);
        var decrypted = rsa.Decrypt(Convert.FromBase64String(secret), false);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    /// RSA加密 不限长度的加密版本
    /// </summary>
    /// <param name="xmlPublicKey">公匙</param>
    /// <param name="content">需要进行加密的字符串</param>
    /// <param name="secret">加密后的Base64字符串</param>
    public static void Encrypt(string xmlPublicKey, string content, out string secret)
    {
        if (string.IsNullOrEmpty(content)) throw new("加密字符串不能为空.");
        if (string.IsNullOrWhiteSpace(xmlPublicKey)) throw new ArgumentException("错误的公匙");
        using var rsaProvider = new RSACryptoServiceProvider();
        var inputBytes = Convert.FromBase64String(content); //有含义的字符串转化为字节流
        rsaProvider.FromXmlString(xmlPublicKey);            //载入公钥
#pragma warning disable IDE0048                             // 为清楚起见，请添加括号
        var bufferSize = rsaProvider.KeySize / 8 - 11;      //单块最大长度
#pragma warning restore IDE0048                             // 为清楚起见，请添加括号
        var buffer = new byte[bufferSize];
        using MemoryStream ms = new(inputBytes), os = new();
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
        secret = Convert.ToBase64String(os.ToArray()); //转化为字节流方便传输
    }

    /// <summary>
    /// RSA解密 不限长度的解密版本
    /// </summary>
    /// <param name="xmlPrivateKey">私匙</param>
    /// <param name="secret">需要进行解密的Base64字符串</param>
    /// <param name="context">解密后的字符串</param>
    public static void Decrypt(string xmlPrivateKey, string secret, out string context)
    {
        if (string.IsNullOrEmpty(secret)) throw new("解密字符串不能为空.");
        if (string.IsNullOrWhiteSpace(xmlPrivateKey)) throw new ArgumentException("错误的私匙");
        using var rsaProvider = new RSACryptoServiceProvider();
        var inputBytes = Convert.FromBase64String(secret);
        rsaProvider.FromXmlString(xmlPrivateKey);
        var bufferSize = rsaProvider.KeySize / 8;
        var buffer = new byte[bufferSize];
        using MemoryStream ms = new(inputBytes), os = new();
        while (true)
        {
            var readSize = ms.Read(buffer, 0, bufferSize);
            if (readSize <= 0) break;
            var temp = new byte[readSize];
            Array.Copy(buffer, 0, temp, 0, readSize);
            var rawBytes = rsaProvider.Decrypt(temp, false);
            os.Write(rawBytes, 0, rawBytes.Length);
        }
        context = Encoding.UTF8.GetString(os.ToArray());
    }

    /// <summary>
    /// 从文件中取得SHA256描述信息
    /// </summary>
    /// <param name="objFile"></param>
    /// <returns></returns>
    public static string GetFileSHA256(FileStream objFile)
    {
        ArgumentNullException.ThrowIfNull(objFile, nameof(objFile));
        var array = SHA256.Create().ComputeHash(objFile);
        objFile.Close();
        return Convert.ToBase64String(array);
    }

    #region RSA签名与签名验证

    /// <summary>
    /// RSA签名
    /// </summary>
    /// <param name="xmlPrivateKey">当前RSA对象的密匙XML字符串(包括专用参数)--私钥</param>
    /// <param name="context">需要签名的字符串</param>
    /// <returns>签名后字符串</returns>
    public static string Signature(string xmlPrivateKey, string context)
    {
        using var RSA = new RSACryptoServiceProvider();
        RSA.FromXmlString(xmlPrivateKey);
        var RSAFormatter = new RSAPKCS1SignatureFormatter(RSA);
        //设置签名的算法为SHA256
        RSAFormatter.SetHashAlgorithm("SHA256");
        //执行签名 
        var encrypt = RSAFormatter.CreateSignature(Convert.FromBase64String(context));
        return Convert.ToBase64String(encrypt);
    }

    /// <summary>
    /// RSA 签名验证
    /// </summary>
    /// <param name="xmlPublicKey">当前RSA对象的密匙XML字符串(不包括专用参数)--公钥</param>
    /// <param name="secret">用RSA签名的字符串数据[俗称:Hash描述字符串,即:MD5或者SHA256这种.本库使用SHA256]</param>
    /// <param name="SignatureString">要为该数据验证的已签名字符串[俗称:签名后的字符串]</param>
    /// <returns>如果 Verification 与使用指定的哈希算法和密钥在 SignatureString 上计算出的签名匹配,则为 true;否则为 false.</returns>
    public static bool Verification(string xmlPublicKey, string secret, string SignatureString)
    {
        using var RSA = new RSACryptoServiceProvider();
        RSA.FromXmlString(xmlPublicKey);
        var formatter = new RSAPKCS1SignatureDeformatter(RSA);
        //指定解密的时候HASH算法为SHA256
        formatter.SetHashAlgorithm("SHA256");
        return formatter.VerifySignature(Convert.FromBase64String(secret), Convert.FromBase64String(SignatureString));
    }

    #endregion
}
using System.Security.Cryptography;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">RSA algorithm</para>
///     <para xml:lang="zh">RSA算法</para>
/// </summary>
public static class RsaCrypt
{
    /// <summary>
    ///     <para xml:lang="en">Create RSA key pair</para>
    ///     <para xml:lang="zh">创建RSA密钥对</para>
    /// </summary>
    /// <param name="keySize">
    ///     <para xml:lang="en">Key size, must be between 384 and 16384 bits, in increments of 8</para>
    ///     <para xml:lang="zh">密钥的大小,必须384位到16384位,增量为8</para>
    /// </param>
    public static RsaSecretKey GenerateKey(int keySize)
    {
        if (keySize is > 16384 or < 384 && keySize % 8 is not 0)
        {
            throw new ArgumentException("密钥的大小,必须384位到16384位,增量为8", nameof(keySize));
        }
        using var rsa = new RSACryptoServiceProvider(keySize);
        rsa.KeySize = keySize;
        rsa.PersistKeyInCsp = false;
        return new()
        {
            PrivateKey = rsa.ToXmlString(true),
            PublicKey = rsa.ToXmlString(false)
        };
    }

    /// <summary>
    ///     <para xml:lang="en">Create RSA key pair</para>
    ///     <para xml:lang="zh">创建RSA密钥对</para>
    /// </summary>
    /// <param name="keySize">
    ///     <para xml:lang="en">Key size, providing common length enumeration</para>
    ///     <para xml:lang="zh">密钥的大小,提供常用长度枚举</para>
    /// </param>
    public static RsaSecretKey GenerateKey(ERsaKeyLength keySize) => GenerateKey((int)keySize);

    /// <summary>
    ///     <para xml:lang="en">Encrypt using RSA (this method has length limitations)</para>
    ///     <para xml:lang="zh">使用RSA的加密byte[](该方法存在长度限制)</para>
    /// </summary>
    /// <param name="xmlPublicKey">
    ///     <para xml:lang="en">XML string of the RSA public key</para>
    ///     <para xml:lang="zh">当前RSA对象的密匙XML字符串(不包括专用参数)--公钥</para>
    /// </param>
    /// <param name="content">
    ///     <para xml:lang="en">Byte array to be encrypted</para>
    ///     <para xml:lang="zh">需要进行加密的字节数组</para>
    /// </param>
    public static byte[] Encrypt(string xmlPublicKey, ReadOnlySpan<byte> content)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPublicKey);
        return rsa.Encrypt(content.ToArray(), false);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt using RSA (this method has length limitations)</para>
    ///     <para xml:lang="zh">RSA解密byte[](该方法存在长度限制)</para>
    /// </summary>
    /// <param name="xmlPrivateKey">
    ///     <para xml:lang="en">XML string of the RSA private key</para>
    ///     <para xml:lang="zh">当前RSA对象的密匙XML字符串(包括专用参数)--私钥</para>
    /// </param>
    /// <param name="secret">
    ///     <para xml:lang="en">Byte array to be decrypted</para>
    ///     <para xml:lang="zh">需要进行解密的字节数组</para>
    /// </param>
    public static byte[] Decrypt(string xmlPrivateKey, ReadOnlySpan<byte> secret)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPrivateKey);
        return rsa.Decrypt(secret.ToArray(), false);
    }

    /// <summary>
    ///     <para xml:lang="en">RSA encryption without length limitation</para>
    ///     <para xml:lang="zh">RSA加密 不限长度的加密版本</para>
    /// </summary>
    /// <param name="xmlPublicKey">
    ///     <para xml:lang="en">Public key</para>
    ///     <para xml:lang="zh">公匙</para>
    /// </param>
    /// <param name="content">
    ///     <para xml:lang="en">Data to be encrypted</para>
    ///     <para xml:lang="zh">需要进行加密的数据</para>
    /// </param>
    /// <param name="secret">
    ///     <para xml:lang="en">Encrypted data</para>
    ///     <para xml:lang="zh">加密后的数据</para>
    /// </param>
    public static void Encrypt(string xmlPublicKey, ReadOnlySpan<byte> content, out byte[] secret)
    {
        if (content.Length is 0) throw new("加密字符串不能为空.");
        ArgumentException.ThrowIfNullOrEmpty(xmlPublicKey, nameof(xmlPublicKey));
        using var rsaProvider = new RSACryptoServiceProvider();
        rsaProvider.FromXmlString(xmlPublicKey);          //载入公钥
        var bufferSize = (rsaProvider.KeySize >> 3) - 11; //单块最大长度
        var buffer = bufferSize <= 256 ? stackalloc byte[bufferSize] : new byte[bufferSize];
        using MemoryStream ms = new(content.ToArray()), os = new();
        while (true)
        {
            //分段加密
            var readSize = ms.Read(buffer);
            if (readSize <= 0) break;
            var temp = buffer[..readSize].ToArray();
            var encryptedBytes = rsaProvider.Encrypt(temp, false);
            os.Write(encryptedBytes, 0, encryptedBytes.Length);
        }
        secret = os.ToArray(); //转化为字节流方便传输
    }

    /// <summary>
    ///     <para xml:lang="en">RSA decryption without length limitation</para>
    ///     <para xml:lang="zh">RSA解密 不限长度的解密版本</para>
    /// </summary>
    /// <param name="xmlPrivateKey">
    ///     <para xml:lang="en">Private key</para>
    ///     <para xml:lang="zh">私匙</para>
    /// </param>
    /// <param name="secret">
    ///     <para xml:lang="en">Data to be decrypted</para>
    ///     <para xml:lang="zh">需要进行解密的数据</para>
    /// </param>
    /// <param name="context">
    ///     <para xml:lang="en">Decrypted data</para>
    ///     <para xml:lang="zh">解密后的数据</para>
    /// </param>
    public static void Decrypt(string xmlPrivateKey, ReadOnlySpan<byte> secret, out byte[] context)
    {
        if (secret.Length is 0) throw new("解密字符串不能为空.");
        ArgumentException.ThrowIfNullOrEmpty(xmlPrivateKey, nameof(xmlPrivateKey));
        using var rsaProvider = new RSACryptoServiceProvider();
        rsaProvider.FromXmlString(xmlPrivateKey);
        var bufferSize = rsaProvider.KeySize >> 3;
        var buffer = bufferSize <= 256 ? stackalloc byte[bufferSize] : new byte[bufferSize];
        using MemoryStream ms = new(secret.ToArray()), os = new();
        while (true)
        {
            var readSize = ms.Read(buffer);
            if (readSize <= 0) break;
            var temp = buffer[..readSize].ToArray();
            var rawBytes = rsaProvider.Decrypt(temp, false);
            os.Write(rawBytes, 0, rawBytes.Length);
        }
        context = os.ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Get SHA256 hash from file</para>
    ///     <para xml:lang="zh">从文件中取得SHA256描述信息</para>
    /// </summary>
    /// <param name="objFile">
    ///     <para xml:lang="en">File stream</para>
    ///     <para xml:lang="zh">文件流</para>
    /// </param>
    public static string GetFileSHA256(FileStream objFile)
    {
        ArgumentNullException.ThrowIfNull(objFile, nameof(objFile));
        using var stream = new MemoryStream();
        objFile.CopyTo(stream);
        var bytes = stream.ToArray();
        var array = SHA256.HashData(bytes);
        objFile.Close();
        return Convert.ToHexString(array);
    }

    #region RSA签名与签名验证

    /// <summary>
    ///     <para xml:lang="en">RSA signature</para>
    ///     <para xml:lang="zh">RSA签名</para>
    /// </summary>
    /// <param name="xmlPrivateKey">
    ///     <para xml:lang="en">XML string of the RSA private key</para>
    ///     <para xml:lang="zh">当前RSA对象的密匙XML字符串(包括专用参数)--私钥</para>
    /// </param>
    /// <param name="context">
    ///     <para xml:lang="en">Data to be signed</para>
    ///     <para xml:lang="zh">需要签名的数据</para>
    /// </param>
    public static byte[] Signature(string xmlPrivateKey, ReadOnlySpan<byte> context)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPrivateKey);
        var RSAFormatter = new RSAPKCS1SignatureFormatter(rsa);
        //设置签名的算法为SHA256
        RSAFormatter.SetHashAlgorithm("SHA256");
        //执行签名 
        return RSAFormatter.CreateSignature(context.ToArray());
    }

    /// <summary>
    ///     <para xml:lang="en">RSA signature verification</para>
    ///     <para xml:lang="zh">RSA 签名验证</para>
    /// </summary>
    /// <param name="xmlPublicKey">
    ///     <para xml:lang="en">XML string of the RSA public key</para>
    ///     <para xml:lang="zh">当前RSA对象的密匙XML字符串(不包括专用参数)--公钥</para>
    /// </param>
    /// <param name="secret">
    ///     <para xml:lang="en">Data signed with RSA (hash string, e.g., MD5 or SHA256, this library uses SHA256)</para>
    ///     <para xml:lang="zh">用RSA签名的数据[俗称:Hash描述字符串,即:MD5或者SHA256这种.本库使用SHA256]</para>
    /// </param>
    /// <param name="signature">
    ///     <para xml:lang="en">Signature to be verified</para>
    ///     <para xml:lang="zh">要为该数据验证的已签名数据</para>
    /// </param>
    public static bool Verification(string xmlPublicKey, ReadOnlySpan<byte> secret, ReadOnlySpan<byte> signature)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPublicKey);
        var formatter = new RSAPKCS1SignatureDeformatter(rsa);
        //指定解密的时候HASH算法为SHA256
        formatter.SetHashAlgorithm("SHA256");
        return formatter.VerifySignature(secret.ToArray(), signature.ToArray());
    }

    #endregion
}
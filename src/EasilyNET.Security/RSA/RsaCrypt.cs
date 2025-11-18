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
        if (keySize is > 16384 or < 384 || keySize % 8 is not 0)
        {
            throw new ArgumentException("密钥的大小,必须384位到16384位,增量为8", nameof(keySize));
        }
        using var rsa = RSA.Create(keySize);
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
    /// <param name="padding">
    ///     <para xml:lang="en">Encryption padding mode (default: OaepSHA256, recommended for security)</para>
    ///     <para xml:lang="zh">加密填充模式(默认:OaepSHA256,推荐使用以提高安全性)</para>
    /// </param>
    public static byte[] Encrypt(string xmlPublicKey, ReadOnlySpan<byte> content, ERsaEncryptionPadding padding = ERsaEncryptionPadding.OaepSHA256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPublicKey);
        if (content.Length is 0)
        {
            throw new ArgumentException("加密内容不能为空.", nameof(content));
        }
        using var rsa = RSA.Create();
        rsa.FromXmlString(xmlPublicKey);
        return rsa.Encrypt(content.ToArray(), GetEncryptionPadding(padding));
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
    /// <param name="padding">
    ///     <para xml:lang="en">Decryption padding mode (must match encryption setting)</para>
    ///     <para xml:lang="zh">解密填充模式(必须与加密时的设置一致)</para>
    /// </param>
    public static byte[] Decrypt(string xmlPrivateKey, ReadOnlySpan<byte> secret, ERsaEncryptionPadding padding = ERsaEncryptionPadding.OaepSHA256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPrivateKey);
        if (secret.Length is 0)
        {
            throw new ArgumentException("解密内容不能为空.", nameof(secret));
        }
        using var rsa = RSA.Create();
        rsa.FromXmlString(xmlPrivateKey);
        return rsa.Decrypt(secret.ToArray(), GetEncryptionPadding(padding));
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
    /// <param name="padding">
    ///     <para xml:lang="en">Encryption padding mode (default: OaepSHA256, recommended for security)</para>
    ///     <para xml:lang="zh">加密填充模式(默认:OaepSHA256,推荐使用以提高安全性)</para>
    /// </param>
    public static void Encrypt(string xmlPublicKey, ReadOnlySpan<byte> content, out byte[] secret, ERsaEncryptionPadding padding = ERsaEncryptionPadding.OaepSHA256)
    {
        if (content.Length is 0)
        {
            throw new ArgumentException("加密字符串不能为空.", nameof(content));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPublicKey);
        using var rsaProvider = RSA.Create();
        rsaProvider.FromXmlString(xmlPublicKey); //载入公钥
        var rsaPadding = GetEncryptionPadding(padding);
        // Calculate buffer size based on padding type
        var bufferSize = GetEncryptionBufferSize(rsaProvider.KeySize, padding);
        var buffer = bufferSize <= 256 ? stackalloc byte[bufferSize] : new byte[bufferSize];
        using MemoryStream ms = new(content.ToArray()), os = new();
        while (true)
        {
            //分段加密
            var readSize = ms.Read(buffer);
            if (readSize <= 0)
            {
                break;
            }
            var temp = buffer[..readSize].ToArray();
            var encryptedBytes = rsaProvider.Encrypt(temp, rsaPadding);
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
    /// <param name="padding">
    ///     <para xml:lang="en">Decryption padding mode (must match encryption setting)</para>
    ///     <para xml:lang="zh">解密填充模式(必须与加密时的设置一致)</para>
    /// </param>
    public static void Decrypt(string xmlPrivateKey, ReadOnlySpan<byte> secret, out byte[] context, ERsaEncryptionPadding padding = ERsaEncryptionPadding.OaepSHA256)
    {
        if (secret.Length is 0)
        {
            throw new ArgumentException("解密字符串不能为空.", nameof(secret));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPrivateKey);
        using var rsaProvider = RSA.Create();
        rsaProvider.FromXmlString(xmlPrivateKey);
        var bufferSize = rsaProvider.KeySize >> 3;
        var buffer = bufferSize <= 256 ? stackalloc byte[bufferSize] : new byte[bufferSize];
        var rsaPadding = GetEncryptionPadding(padding);
        using MemoryStream ms = new(secret.ToArray()), os = new();
        while (true)
        {
            var readSize = ms.Read(buffer);
            if (readSize <= 0)
            {
                break;
            }
            var temp = buffer[..readSize].ToArray();
            var rawBytes = rsaProvider.Decrypt(temp, rsaPadding);
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
        ArgumentNullException.ThrowIfNull(objFile);
        if (!objFile.CanRead)
        {
            throw new ArgumentException("文件流不可读.", nameof(objFile));
        }
        var originalPosition = objFile.Position;
        objFile.Position = 0;
        var array = SHA256.HashData(objFile);
        objFile.Position = originalPosition; // 恢复原始位置
        return Convert.ToHexString(array);
    }

    /// <summary>
    ///     <para xml:lang="en">Get SHA256 hash from file asynchronously</para>
    ///     <para xml:lang="zh">异步从文件中取得SHA256描述信息</para>
    /// </summary>
    /// <param name="objFile">
    ///     <para xml:lang="en">File stream</para>
    ///     <para xml:lang="zh">文件流</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    public static async Task<string> GetFileSHA256Async(FileStream objFile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objFile);
        if (!objFile.CanRead)
        {
            throw new ArgumentException("文件流不可读.", nameof(objFile));
        }
        var originalPosition = objFile.Position;
        objFile.Position = 0;
        var array = await SHA256.HashDataAsync(objFile, cancellationToken);
        objFile.Position = originalPosition; // 恢复原始位置
        return Convert.ToHexString(array);
    }

    /// <summary>
    ///     <para xml:lang="en">Export RSA private key to PEM format</para>
    ///     <para xml:lang="zh">导出RSA私钥到PEM格式</para>
    /// </summary>
    /// <param name="xmlPrivateKey">
    ///     <para xml:lang="en">XML private key</para>
    ///     <para xml:lang="zh">XML私钥</para>
    /// </param>
    public static string ExportPrivateKeyToPem(string xmlPrivateKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPrivateKey);
        using var rsa = RSA.Create();
        rsa.FromXmlString(xmlPrivateKey);
        return rsa.ExportRSAPrivateKeyPem();
    }

    /// <summary>
    ///     <para xml:lang="en">Export RSA public key to PEM format</para>
    ///     <para xml:lang="zh">导出RSA公钥到PEM格式</para>
    /// </summary>
    /// <param name="xmlPublicKey">
    ///     <para xml:lang="en">XML public key</para>
    ///     <para xml:lang="zh">XML公钥</para>
    /// </param>
    public static string ExportPublicKeyToPem(string xmlPublicKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPublicKey);
        using var rsa = RSA.Create();
        rsa.FromXmlString(xmlPublicKey);
        return rsa.ExportRSAPublicKeyPem();
    }

    /// <summary>
    ///     <para xml:lang="en">Import RSA private key from PEM format</para>
    ///     <para xml:lang="zh">从PEM格式导入RSA私钥</para>
    /// </summary>
    /// <param name="pemPrivateKey">
    ///     <para xml:lang="en">PEM private key</para>
    ///     <para xml:lang="zh">PEM私钥</para>
    /// </param>
    public static string ImportPrivateKeyFromPem(string pemPrivateKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pemPrivateKey);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pemPrivateKey);
        return rsa.ToXmlString(true);
    }

    /// <summary>
    ///     <para xml:lang="en">Import RSA public key from PEM format</para>
    ///     <para xml:lang="zh">从PEM格式导入RSA公钥</para>
    /// </summary>
    /// <param name="pemPublicKey">
    ///     <para xml:lang="en">PEM public key</para>
    ///     <para xml:lang="zh">PEM公钥</para>
    /// </param>
    public static string ImportPublicKeyFromPem(string pemPublicKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pemPublicKey);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pemPublicKey);
        return rsa.ToXmlString(false);
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
    /// <param name="hashAlgorithm">
    ///     <para xml:lang="en">Hash algorithm name (default: SHA256)</para>
    ///     <para xml:lang="zh">哈希算法名称(默认:SHA256)</para>
    /// </param>
    /// <param name="padding">
    ///     <para xml:lang="en">Signature padding mode (default: Pkcs1, widely compatible)</para>
    ///     <para xml:lang="zh">签名填充模式(默认:Pkcs1,广泛兼容)</para>
    /// </param>
    public static byte[] Signature(string xmlPrivateKey, ReadOnlySpan<byte> context, HashAlgorithmName hashAlgorithm = default, ERsaSignaturePadding padding = ERsaSignaturePadding.Pkcs1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPrivateKey);
        if (context.Length is 0)
        {
            throw new ArgumentException("签名数据不能为空.", nameof(context));
        }
        using var rsa = RSA.Create();
        rsa.FromXmlString(xmlPrivateKey);
        // Use SHA256 as default if not specified
        var algorithm = hashAlgorithm == default ? HashAlgorithmName.SHA256 : hashAlgorithm;
        var hash = ComputeHash(context, algorithm);
        return rsa.SignHash(hash, algorithm, GetSignaturePadding(padding));
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
    ///     <para xml:lang="en">Original data (will be hashed)</para>
    ///     <para xml:lang="zh">原始数据(将进行哈希)</para>
    /// </param>
    /// <param name="signature">
    ///     <para xml:lang="en">Signature to be verified</para>
    ///     <para xml:lang="zh">要为该数据验证的已签名数据</para>
    /// </param>
    /// <param name="hashAlgorithm">
    ///     <para xml:lang="en">Hash algorithm name (default: SHA256, must match signature setting)</para>
    ///     <para xml:lang="zh">哈希算法名称(默认:SHA256,必须与签名时一致)</para>
    /// </param>
    /// <param name="padding">
    ///     <para xml:lang="en">Signature padding mode (default: Pkcs1, must match signature setting)</para>
    ///     <para xml:lang="zh">签名填充模式(默认:Pkcs1,必须与签名时一致)</para>
    /// </param>
    public static bool Verification(string xmlPublicKey, ReadOnlySpan<byte> secret, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm = default, ERsaSignaturePadding padding = ERsaSignaturePadding.Pkcs1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPublicKey);
        if (secret.Length is 0)
        {
            throw new ArgumentException("验证数据不能为空.", nameof(secret));
        }
        if (signature.Length is 0)
        {
            throw new ArgumentException("签名不能为空.", nameof(signature));
        }
        using var rsa = RSA.Create();
        rsa.FromXmlString(xmlPublicKey);
        // Use SHA256 as default if not specified
        var algorithm = hashAlgorithm == default ? HashAlgorithmName.SHA256 : hashAlgorithm;
        var hash = ComputeHash(secret, algorithm);
        return rsa.VerifyHash(hash, signature.ToArray(), algorithm, GetSignaturePadding(padding));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     <para xml:lang="en">Convert ERsaEncryptionPadding to RSAEncryptionPadding</para>
    ///     <para xml:lang="zh">将ERsaEncryptionPadding转换为RSAEncryptionPadding</para>
    /// </summary>
    private static RSAEncryptionPadding GetEncryptionPadding(ERsaEncryptionPadding padding) =>
        padding switch
        {
            ERsaEncryptionPadding.Pkcs1      => RSAEncryptionPadding.Pkcs1,
            ERsaEncryptionPadding.OaepSHA1   => RSAEncryptionPadding.OaepSHA1,
            ERsaEncryptionPadding.OaepSHA256 => RSAEncryptionPadding.OaepSHA256,
            ERsaEncryptionPadding.OaepSHA384 => RSAEncryptionPadding.OaepSHA384,
            ERsaEncryptionPadding.OaepSHA512 => RSAEncryptionPadding.OaepSHA512,
            _                                => throw new ArgumentOutOfRangeException(nameof(padding), padding, "不支持的加密填充模式")
        };

    /// <summary>
    ///     <para xml:lang="en">Convert ERsaSignaturePadding to RSASignaturePadding</para>
    ///     <para xml:lang="zh">将ERsaSignaturePadding转换为RSASignaturePadding</para>
    /// </summary>
    private static RSASignaturePadding GetSignaturePadding(ERsaSignaturePadding padding) =>
        padding switch
        {
            ERsaSignaturePadding.Pkcs1 => RSASignaturePadding.Pkcs1,
            ERsaSignaturePadding.Pss   => RSASignaturePadding.Pss,
            _                          => throw new ArgumentOutOfRangeException(nameof(padding), padding, "不支持的签名填充模式")
        };

    /// <summary>
    ///     <para xml:lang="en">Calculate encryption buffer size based on key size and padding</para>
    ///     <para xml:lang="zh">根据密钥大小和填充模式计算加密缓冲区大小</para>
    /// </summary>
    private static int GetEncryptionBufferSize(int keySize, ERsaEncryptionPadding padding) =>
        padding switch
        {
            ERsaEncryptionPadding.Pkcs1      => (keySize >> 3) - 11,
            ERsaEncryptionPadding.OaepSHA1   => (keySize >> 3) - 42,
            ERsaEncryptionPadding.OaepSHA256 => (keySize >> 3) - 66,
            ERsaEncryptionPadding.OaepSHA384 => (keySize >> 3) - 98,
            ERsaEncryptionPadding.OaepSHA512 => (keySize >> 3) - 130,
            _                                => throw new ArgumentOutOfRangeException(nameof(padding), padding, "不支持的加密填充模式")
        };

    /// <summary>
    ///     <para xml:lang="en">Compute hash using specified algorithm</para>
    ///     <para xml:lang="zh">使用指定算法计算哈希</para>
    /// </summary>
    private static byte[] ComputeHash(ReadOnlySpan<byte> data, HashAlgorithmName algorithmName)
    {
        if (algorithmName == HashAlgorithmName.SHA256)
        {
            return SHA256.HashData(data);
        }
        if (algorithmName == HashAlgorithmName.SHA1)
        {
            return SHA1.HashData(data);
        }
        if (algorithmName == HashAlgorithmName.SHA384)
        {
            return SHA384.HashData(data);
        }
        if (algorithmName == HashAlgorithmName.SHA512)
        {
            return SHA512.HashData(data);
        }
        return algorithmName == HashAlgorithmName.MD5 ? MD5.HashData(data) : throw new ArgumentException($"不支持的哈希算法: {algorithmName.Name}", nameof(algorithmName));
    }

    #endregion
}
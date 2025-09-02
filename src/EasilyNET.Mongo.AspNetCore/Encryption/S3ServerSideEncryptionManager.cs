using System.Security.Cryptography;
using System.Text;

namespace EasilyNET.Mongo.AspNetCore.Encryption;

/// <summary>
///     <para xml:lang="en">S3 Server-Side Encryption Manager</para>
///     <para xml:lang="zh">S3服务器端加密管理器</para>
/// </summary>
public class S3ServerSideEncryptionManager(string masterKey)
{
    private readonly Dictionary<string, EncryptionKey> _encryptionKeys = new();

    /// <summary>
    ///     <para xml:lang="en">Encrypt data stream</para>
    ///     <para xml:lang="zh">加密数据流</para>
    /// </summary>
    public async Task<(Stream EncryptedStream, string KeyId, string EncryptedKey)> EncryptAsync(Stream inputStream, string algorithm = "AES256")
    {
        // Generate a unique key for this object
        var objectKey = GenerateObjectKey();

        // Encrypt the object key with master key
        var encryptedObjectKey = EncryptKey(objectKey, masterKey);

        // Create encryption key entry
        var keyId = Guid.NewGuid().ToString();
        _encryptionKeys[keyId] = new()
        {
            KeyId = keyId,
            EncryptedObjectKey = encryptedObjectKey,
            Algorithm = algorithm,
            Created = DateTime.UtcNow
        };

        // Encrypt the data
        var encryptedStream = await EncryptStreamAsync(inputStream, objectKey);
        return (encryptedStream, keyId, encryptedObjectKey);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt data stream</para>
    ///     <para xml:lang="zh">解密数据流</para>
    /// </summary>
    public async Task<Stream> DecryptAsync(Stream encryptedStream, string keyId)
    {
        if (!_encryptionKeys.TryGetValue(keyId, out var encryptionKey))
        {
            throw new ArgumentException("Encryption key not found");
        }

        // Decrypt the object key
        var objectKey = DecryptKey(encryptionKey.EncryptedObjectKey, masterKey);

        // Decrypt the data
        return await DecryptStreamAsync(encryptedStream, objectKey);
    }

    /// <summary>
    ///     <para xml:lang="en">Get encryption information</para>
    ///     <para xml:lang="zh">获取加密信息</para>
    /// </summary>
    public EncryptionInfo? GetEncryptionInfo(string keyId)
    {
        if (!_encryptionKeys.TryGetValue(keyId, out var key))
        {
            return null;
        }
        return new()
        {
            KeyId = key.KeyId,
            Algorithm = key.Algorithm,
            EncryptedKey = key.EncryptedObjectKey
        };
    }

    /// <summary>
    ///     <para xml:lang="en">Remove encryption key</para>
    ///     <para xml:lang="zh">移除加密密钥</para>
    /// </summary>
    public bool RemoveEncryptionKey(string keyId) => _encryptionKeys.Remove(keyId);

    private static string GenerateObjectKey()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }

    private static string EncryptKey(string objectKey, string masterKey)
    {
        using var aes = Aes.Create();
        var keyBytes = Encoding.UTF8.GetBytes(masterKey);
        Array.Resize(ref keyBytes, 32); // Ensure 256-bit key
        aes.Key = keyBytes;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(objectKey);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string DecryptKey(string encryptedKey, string masterKey)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedKey);
        using var aes = Aes.Create();
        var keyBytes = Encoding.UTF8.GetBytes(masterKey);
        Array.Resize(ref keyBytes, 32);
        aes.Key = keyBytes;
        var iv = new byte[16];
        Array.Copy(encryptedBytes, 0, iv, 0, 16);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encryptedBytes, 16, encryptedBytes.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    private static async Task<Stream> EncryptStreamAsync(Stream inputStream, string objectKey)
    {
        var keyBytes = Convert.FromBase64String(objectKey);
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();
        var outputStream = new MemoryStream();
        outputStream.Write(aes.IV, 0, aes.IV.Length);
        using var encryptor = aes.CreateEncryptor();
        await using var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
        await inputStream.CopyToAsync(cryptoStream);
        await cryptoStream.FlushFinalBlockAsync();
        outputStream.Position = 0;
        return outputStream;
    }

    private static async Task<Stream> DecryptStreamAsync(Stream encryptedStream, string objectKey)
    {
        var keyBytes = Convert.FromBase64String(objectKey);
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        var iv = new byte[16];
        await encryptedStream.ReadExactlyAsync(iv, 0, 16);
        aes.IV = iv;
        var outputStream = new MemoryStream();
        using var decryptor = aes.CreateDecryptor();
        await using var cryptoStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read);
        await cryptoStream.CopyToAsync(outputStream);
        outputStream.Position = 0;
        return outputStream;
    }
}

/// <summary>
///     <para xml:lang="en">Encryption Key</para>
///     <para xml:lang="zh">加密密钥</para>
/// </summary>
public class EncryptionKey
{
    /// <summary>
    ///     <para xml:lang="en">Key ID</para>
    ///     <para xml:lang="zh">密钥ID</para>
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Encrypted object key</para>
    ///     <para xml:lang="zh">加密的对象密钥</para>
    /// </summary>
    public string EncryptedObjectKey { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Encryption algorithm</para>
    ///     <para xml:lang="zh">加密算法</para>
    /// </summary>
    public string Algorithm { get; set; } = "AES256";

    /// <summary>
    ///     <para xml:lang="en">Creation time</para>
    ///     <para xml:lang="zh">创建时间</para>
    /// </summary>
    public DateTime Created { get; set; }
}

/// <summary>
///     <para xml:lang="en">Encryption Information</para>
///     <para xml:lang="zh">加密信息</para>
/// </summary>
public class EncryptionInfo
{
    /// <summary>
    ///     <para xml:lang="en">Key ID</para>
    ///     <para xml:lang="zh">密钥ID</para>
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Encryption algorithm</para>
    ///     <para xml:lang="zh">加密算法</para>
    /// </summary>
    public string Algorithm { get; set; } = "AES256";

    /// <summary>
    ///     <para xml:lang="en">Encrypted key</para>
    ///     <para xml:lang="zh">加密密钥</para>
    /// </summary>
    public string EncryptedKey { get; set; } = string.Empty;
}

/// <summary>
///     <para xml:lang="en">Server-Side Encryption Configuration</para>
///     <para xml:lang="zh">服务器端加密配置</para>
/// </summary>
public class ServerSideEncryptionConfiguration
{
    /// <summary>
    ///     <para xml:lang="en">Whether SSE is enabled</para>
    ///     <para xml:lang="zh">是否启用SSE</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Encryption algorithm</para>
    ///     <para xml:lang="zh">加密算法</para>
    /// </summary>
    public string Algorithm { get; set; } = "AES256";

    /// <summary>
    ///     <para xml:lang="en">KMS Key ID (for future use)</para>
    ///     <para xml:lang="zh">KMS密钥ID（未来使用）</para>
    /// </summary>
    public string? KmsKeyId { get; set; }
}
namespace EasilyNET.Mongo.GridFS.S3.Encryption;

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
namespace EasilyNET.Mongo.GridFS.S3.Encryption;

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
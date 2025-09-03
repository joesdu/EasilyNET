namespace EasilyNET.Mongo.GridFS.S3.ObjectResults;

/// <summary>
///     <para xml:lang="en">Put encrypted object result</para>
///     <para xml:lang="zh">上传加密对象结果</para>
/// </summary>
public class PutEncryptedObjectResult
{
    /// <summary>
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">ETag</para>
    ///     <para xml:lang="zh">ETag</para>
    /// </summary>
    public string ETag { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Encryption key ID</para>
    ///     <para xml:lang="zh">加密密钥ID</para>
    /// </summary>
    public string? EncryptionKeyId { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Server-side encryption</para>
    ///     <para xml:lang="zh">服务器端加密</para>
    /// </summary>
    public string? ServerSideEncryption { get; set; }
}
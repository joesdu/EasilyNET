namespace EasilyNET.Mongo.GridFS.S3.Versioning;

/// <summary>
///     <para xml:lang="en">Object Version</para>
///     <para xml:lang="zh">对象版本</para>
/// </summary>
public class ObjectVersion
{
    /// <summary>
    ///     <para xml:lang="en">Version ID</para>
    ///     <para xml:lang="zh">版本ID</para>
    /// </summary>
    public string VersionId { get; set; } = string.Empty;

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
    ///     <para xml:lang="en">Content type</para>
    ///     <para xml:lang="zh">内容类型</para>
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Size in bytes</para>
    ///     <para xml:lang="zh">大小（字节）</para>
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Last modified</para>
    ///     <para xml:lang="zh">最后修改时间</para>
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    ///     <para xml:lang="en">ETag</para>
    ///     <para xml:lang="zh">ETag</para>
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Is latest version</para>
    ///     <para xml:lang="zh">是否为最新版本</para>
    /// </summary>
    public bool IsLatest { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Custom metadata</para>
    ///     <para xml:lang="zh">自定义元数据</para>
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    ///     <para xml:lang="en">Is delete marker</para>
    ///     <para xml:lang="zh">是否为删除标记</para>
    /// </summary>
    public bool IsDeleteMarker { get; set; }
}
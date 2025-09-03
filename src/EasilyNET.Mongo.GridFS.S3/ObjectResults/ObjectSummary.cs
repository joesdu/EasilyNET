namespace EasilyNET.Mongo.GridFS.S3.ObjectResults;

/// <summary>
///     <para xml:lang="en">Object summary</para>
///     <para xml:lang="zh">对象摘要</para>
/// </summary>
public class ObjectSummary
{
    /// <summary>
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Last modified</para>
    ///     <para xml:lang="zh">最后修改时间</para>
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Size</para>
    ///     <para xml:lang="zh">大小</para>
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    ///     <para xml:lang="en">ETag</para>
    ///     <para xml:lang="zh">ETag</para>
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Storage class</para>
    ///     <para xml:lang="zh">存储类</para>
    /// </summary>
    public string StorageClass { get; set; } = "STANDARD";
}
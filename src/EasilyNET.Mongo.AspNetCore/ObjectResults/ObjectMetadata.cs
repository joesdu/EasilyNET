namespace EasilyNET.Mongo.AspNetCore.ObjectResults;

/// <summary>
///     <para xml:lang="en">Object metadata</para>
///     <para xml:lang="zh">对象元数据</para>
/// </summary>
public class ObjectMetadata
{
    /// <summary>
    ///     <para xml:lang="en">Content type</para>
    ///     <para xml:lang="zh">内容类型</para>
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Content length</para>
    ///     <para xml:lang="zh">内容长度</para>
    /// </summary>
    public long ContentLength { get; set; }

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
    ///     <para xml:lang="en">Custom metadata</para>
    ///     <para xml:lang="zh">自定义元数据</para>
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
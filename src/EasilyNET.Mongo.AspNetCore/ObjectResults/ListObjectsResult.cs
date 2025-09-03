namespace EasilyNET.Mongo.AspNetCore.ObjectResults;

/// <summary>
///     <para xml:lang="en">List objects result</para>
///     <para xml:lang="zh">列出对象结果</para>
/// </summary>
public class ListObjectsResult
{
    /// <summary>
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Prefix</para>
    ///     <para xml:lang="zh">前缀</para>
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Marker</para>
    ///     <para xml:lang="zh">标记</para>
    /// </summary>
    public string? Marker { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Next marker</para>
    ///     <para xml:lang="zh">下一个标记</para>
    /// </summary>
    public string? NextMarker { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Maximum keys</para>
    ///     <para xml:lang="zh">最大键数</para>
    /// </summary>
    public int MaxKeys { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Is truncated</para>
    ///     <para xml:lang="zh">是否截断</para>
    /// </summary>
    public bool IsTruncated { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Object summaries</para>
    ///     <para xml:lang="zh">对象摘要</para>
    /// </summary>
    public List<ObjectSummary> Objects { get; set; } = [];
}
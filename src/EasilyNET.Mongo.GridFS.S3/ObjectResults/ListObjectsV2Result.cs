namespace EasilyNET.Mongo.GridFS.S3.ObjectResults;

/// <summary>
///     <para xml:lang="en">List objects V2 result</para>
///     <para xml:lang="zh">列出对象V2结果</para>
/// </summary>
public class ListObjectsV2Result
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
    ///     <para xml:lang="en">Continuation token</para>
    ///     <para xml:lang="zh">延续令牌</para>
    /// </summary>
    public string? ContinuationToken { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Next continuation token</para>
    ///     <para xml:lang="zh">下一个延续令牌</para>
    /// </summary>
    public string? NextContinuationToken { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Start after</para>
    ///     <para xml:lang="zh">开始于</para>
    /// </summary>
    public string? StartAfter { get; set; }

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

    /// <summary>
    ///     <para xml:lang="en">Common prefixes</para>
    ///     <para xml:lang="zh">公共前缀</para>
    /// </summary>
    public List<string> CommonPrefixes { get; set; } = [];
}
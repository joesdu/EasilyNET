namespace EasilyNET.Mongo.GridFS.S3.ObjectResults;

/// <summary>
///     <para xml:lang="en">List buckets result</para>
///     <para xml:lang="zh">列出存储桶结果</para>
/// </summary>
public class ListBucketsResult
{
    /// <summary>
    ///     <para xml:lang="en">Buckets</para>
    ///     <para xml:lang="zh">存储桶</para>
    /// </summary>
    public List<BucketSummary> Buckets { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">Bucket summary</para>
///     <para xml:lang="zh">存储桶摘要</para>
/// </summary>
public class BucketSummary
{
    /// <summary>
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Creation date</para>
    ///     <para xml:lang="zh">创建日期</para>
    /// </summary>
    public DateTime CreationDate { get; set; }
}
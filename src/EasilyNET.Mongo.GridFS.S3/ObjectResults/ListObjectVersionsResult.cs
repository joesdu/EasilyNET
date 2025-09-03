using EasilyNET.Mongo.GridFS.S3.Versioning;

namespace EasilyNET.Mongo.GridFS.S3.ObjectResults;

/// <summary>
///     <para xml:lang="en">List object versions result</para>
///     <para xml:lang="zh">列出对象版本结果</para>
/// </summary>
public class ListObjectVersionsResult
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
    ///     <para xml:lang="en">Versions</para>
    ///     <para xml:lang="zh">版本</para>
    /// </summary>
    public List<ObjectVersion> Versions { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">Delete markers</para>
    ///     <para xml:lang="zh">删除标记</para>
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<ObjectVersion> DeleteMarkers { get; set; } = [];
}
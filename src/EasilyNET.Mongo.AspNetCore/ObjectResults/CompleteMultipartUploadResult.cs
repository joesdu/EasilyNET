namespace EasilyNET.Mongo.AspNetCore.ObjectResults;

/// <summary>
///     <para xml:lang="en">Complete multipart upload result</para>
///     <para xml:lang="zh">完成多部分上传结果</para>
/// </summary>
public class CompleteMultipartUploadResult
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
}
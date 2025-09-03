namespace EasilyNET.Mongo.AspNetCore.ObjectResults;

/// <summary>
///     <para xml:lang="en">Initiate multipart upload result</para>
///     <para xml:lang="zh">初始化多部分上传结果</para>
/// </summary>
public class InitiateMultipartUploadResult
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
    ///     <para xml:lang="en">Upload ID</para>
    ///     <para xml:lang="zh">上传ID</para>
    /// </summary>
    public string UploadId { get; set; } = string.Empty;
}
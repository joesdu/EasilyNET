namespace EasilyNET.Mongo.GridFS.S3.ObjectResults;

/// <summary>
///     <para xml:lang="en">Upload part result</para>
///     <para xml:lang="zh">上传部分结果</para>
/// </summary>
public class UploadPartResult
{
    /// <summary>
    ///     <para xml:lang="en">Part number</para>
    ///     <para xml:lang="zh">部分编号</para>
    /// </summary>
    public int PartNumber { get; set; }

    /// <summary>
    ///     <para xml:lang="en">ETag</para>
    ///     <para xml:lang="zh">ETag</para>
    /// </summary>
    public string ETag { get; set; } = string.Empty;
}
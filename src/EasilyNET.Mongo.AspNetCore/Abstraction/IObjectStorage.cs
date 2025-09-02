// ReSharper disable UnusedParameter.Global

namespace EasilyNET.Mongo.AspNetCore.Abstraction;

/// <summary>
///     <para xml:lang="en">Object storage interface, compatible with S3-style APIs</para>
///     <para xml:lang="zh">对象存储接口，兼容S3风格的API</para>
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    ///     <para xml:lang="en">Upload object</para>
    ///     <para xml:lang="zh">上传对象</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="stream">
    ///     <para xml:lang="en">Object data stream</para>
    ///     <para xml:lang="zh">对象数据流</para>
    /// </param>
    /// <param name="contentType">
    ///     <para xml:lang="en">Content type</para>
    ///     <para xml:lang="zh">内容类型</para>
    /// </param>
    /// <param name="metadata">
    ///     <para xml:lang="en">Metadata</para>
    ///     <para xml:lang="zh">元数据</para>
    /// </param>
    Task PutObjectAsync(string bucketName, string key, Stream stream, string? contentType = null, Dictionary<string, string>? metadata = null);

    /// <summary>
    ///     <para xml:lang="en">Download object</para>
    ///     <para xml:lang="zh">下载对象</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="range">
    ///     <para xml:lang="en">Byte range to download</para>
    ///     <para xml:lang="zh">要下载的字节范围</para>
    /// </param>
    Task<Stream> GetObjectAsync(string bucketName, string key, string? range = null);

    /// <summary>
    ///     <para xml:lang="en">Copy object</para>
    ///     <para xml:lang="zh">复制对象</para>
    /// </summary>
    /// <param name="sourceBucket">
    ///     <para xml:lang="en">Source bucket name</para>
    ///     <para xml:lang="zh">源存储桶名称</para>
    /// </param>
    /// <param name="sourceKey">
    ///     <para xml:lang="en">Source object key</para>
    ///     <para xml:lang="zh">源对象键</para>
    /// </param>
    /// <param name="destinationBucket">
    ///     <para xml:lang="en">Destination bucket name</para>
    ///     <para xml:lang="zh">目标存储桶名称</para>
    /// </param>
    /// <param name="destinationKey">
    ///     <para xml:lang="en">Destination object key</para>
    ///     <para xml:lang="zh">目标对象键</para>
    /// </param>
    /// <param name="metadata">
    ///     <para xml:lang="en">New metadata (optional)</para>
    ///     <para xml:lang="zh">新元数据（可选）</para>
    /// </param>
    Task CopyObjectAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, Dictionary<string, string>? metadata = null);

    /// <summary>
    ///     <para xml:lang="en">Delete object</para>
    ///     <para xml:lang="zh">删除对象</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    Task DeleteObjectAsync(string bucketName, string key);

    /// <summary>
    ///     <para xml:lang="en">Delete multiple objects</para>
    ///     <para xml:lang="zh">批量删除对象</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="keys">
    ///     <para xml:lang="en">Object keys to delete</para>
    ///     <para xml:lang="zh">要删除的对象键</para>
    /// </param>
    Task<DeleteObjectsResult> DeleteObjectsAsync(string bucketName, IEnumerable<string> keys);

    /// <summary>
    ///     <para xml:lang="en">Check if object exists</para>
    ///     <para xml:lang="zh">检查对象是否存在</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    Task<bool> ObjectExistsAsync(string bucketName, string key);

    /// <summary>
    ///     <para xml:lang="en">List objects in bucket</para>
    ///     <para xml:lang="zh">列出存储桶中的对象</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="prefix">
    ///     <para xml:lang="en">Key prefix</para>
    ///     <para xml:lang="zh">键前缀</para>
    /// </param>
    /// <param name="marker">
    ///     <para xml:lang="en">Pagination marker</para>
    ///     <para xml:lang="zh">分页标记</para>
    /// </param>
    /// <param name="maxKeys">
    ///     <para xml:lang="en">Maximum number of keys</para>
    ///     <para xml:lang="zh">最大键数量</para>
    /// </param>
    Task<ListObjectsResult> ListObjectsAsync(string bucketName, string? prefix = null, string? marker = null, int? maxKeys = null);

    /// <summary>
    ///     <para xml:lang="en">Get object metadata</para>
    ///     <para xml:lang="zh">获取对象元数据</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    Task<ObjectMetadata> GetObjectMetadataAsync(string bucketName, string key);

    /// <summary>
    ///     <para xml:lang="en">Initiate multipart upload</para>
    ///     <para xml:lang="zh">初始化多部分上传</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="contentType">
    ///     <para xml:lang="en">Content type</para>
    ///     <para xml:lang="zh">内容类型</para>
    /// </param>
    /// <param name="metadata">
    ///     <para xml:lang="en">Metadata</para>
    ///     <para xml:lang="zh">元数据</para>
    /// </param>
    Task<InitiateMultipartUploadResult> InitiateMultipartUploadAsync(string bucketName, string key, string? contentType = null, Dictionary<string, string>? metadata = null);

    /// <summary>
    ///     <para xml:lang="en">Upload part</para>
    ///     <para xml:lang="zh">上传部分</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="uploadId">
    ///     <para xml:lang="en">Upload ID</para>
    ///     <para xml:lang="zh">上传ID</para>
    /// </param>
    /// <param name="partNumber">
    ///     <para xml:lang="en">Part number</para>
    ///     <para xml:lang="zh">部分编号</para>
    /// </param>
    /// <param name="stream">
    ///     <para xml:lang="en">Part data stream</para>
    ///     <para xml:lang="zh">部分数据流</para>
    /// </param>
    Task<UploadPartResult> UploadPartAsync(string bucketName, string key, string uploadId, int partNumber, Stream stream);

    /// <summary>
    ///     <para xml:lang="en">Complete multipart upload</para>
    ///     <para xml:lang="zh">完成多部分上传</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="uploadId">
    ///     <para xml:lang="en">Upload ID</para>
    ///     <para xml:lang="zh">上传ID</para>
    /// </param>
    /// <param name="parts">
    ///     <para xml:lang="en">Completed parts</para>
    ///     <para xml:lang="zh">已完成的部件</para>
    /// </param>
    Task<CompleteMultipartUploadResult> CompleteMultipartUploadAsync(string bucketName, string key, string uploadId, IEnumerable<PartETag> parts);

    /// <summary>
    ///     <para xml:lang="en">Abort multipart upload</para>
    ///     <para xml:lang="zh">中止多部分上传</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="uploadId">
    ///     <para xml:lang="en">Upload ID</para>
    ///     <para xml:lang="zh">上传ID</para>
    /// </param>
    Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId);

    /// <summary>
    ///     <para xml:lang="en">List objects in bucket (V2)</para>
    ///     <para xml:lang="zh">列出存储桶中的对象（V2）</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="prefix">
    ///     <para xml:lang="en">Key prefix</para>
    ///     <para xml:lang="zh">键前缀</para>
    /// </param>
    /// <param name="continuationToken">
    ///     <para xml:lang="en">Continuation token for pagination</para>
    ///     <para xml:lang="zh">分页的延续令牌</para>
    /// </param>
    /// <param name="maxKeys">
    ///     <para xml:lang="en">Maximum number of keys</para>
    ///     <para xml:lang="zh">最大键数量</para>
    /// </param>
    /// <param name="startAfter">
    ///     <para xml:lang="en">Start listing after this key</para>
    ///     <para xml:lang="zh">从此键之后开始列出</para>
    /// </param>
    Task<ListObjectsV2Result> ListObjectsV2Async(string bucketName, string? prefix = null, string? continuationToken = null, int? maxKeys = null, string? startAfter = null);

    /// <summary>
    ///     <para xml:lang="en">Create bucket</para>
    ///     <para xml:lang="zh">创建存储桶</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    Task CreateBucketAsync(string bucketName);

    /// <summary>
    ///     <para xml:lang="en">Delete bucket</para>
    ///     <para xml:lang="zh">删除存储桶</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    Task DeleteBucketAsync(string bucketName);

    /// <summary>
    ///     <para xml:lang="en">List buckets</para>
    ///     <para xml:lang="zh">列出存储桶</para>
    /// </summary>
    Task<ListBucketsResult> ListBucketsAsync();

    /// <summary>
    ///     <para xml:lang="en">Check if bucket exists</para>
    ///     <para xml:lang="zh">检查存储桶是否存在</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    Task<bool> BucketExistsAsync(string bucketName);

    /// <summary>
    ///     <para xml:lang="en">Check permission for operation</para>
    ///     <para xml:lang="zh">检查操作权限</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="operation">
    ///     <para xml:lang="en">Operation type</para>
    ///     <para xml:lang="zh">操作类型</para>
    /// </param>
    /// <param name="accessKey">
    ///     <para xml:lang="en">Access key</para>
    ///     <para xml:lang="zh">访问密钥</para>
    /// </param>
    Task<bool> CheckPermissionAsync(string bucketName, string key, string operation, string accessKey);

    /// <summary>
    ///     <para xml:lang="en">Get object version</para>
    ///     <para xml:lang="zh">获取对象版本</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="versionId">
    ///     <para xml:lang="en">Version ID</para>
    ///     <para xml:lang="zh">版本ID</para>
    /// </param>
    Task<ObjectVersion> GetObjectVersionAsync(string bucketName, string key, string? versionId = null);

    /// <summary>
    ///     <para xml:lang="en">List object versions</para>
    ///     <para xml:lang="zh">列出对象版本</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="prefix">
    ///     <para xml:lang="en">Key prefix</para>
    ///     <para xml:lang="zh">键前缀</para>
    /// </param>
    Task<ListObjectVersionsResult> ListObjectVersionsAsync(string bucketName, string? prefix = null);

    /// <summary>
    ///     <para xml:lang="en">Put encrypted object</para>
    ///     <para xml:lang="zh">上传加密对象</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="stream">
    ///     <para xml:lang="en">Object data stream</para>
    ///     <para xml:lang="zh">对象数据流</para>
    /// </param>
    /// <param name="contentType">
    ///     <para xml:lang="en">Content type</para>
    ///     <para xml:lang="zh">内容类型</para>
    /// </param>
    /// <param name="metadata">
    ///     <para xml:lang="en">Metadata</para>
    ///     <para xml:lang="zh">元数据</para>
    /// </param>
    /// <param name="encryptionConfig">
    ///     <para xml:lang="en">Server-side encryption configuration</para>
    ///     <para xml:lang="zh">服务器端加密配置</para>
    /// </param>
    Task<PutEncryptedObjectResult> PutEncryptedObjectAsync(string bucketName, string key, Stream stream, string? contentType = null, Dictionary<string, string>? metadata = null, ServerSideEncryptionConfiguration? encryptionConfig = null);

    /// <summary>
    ///     <para xml:lang="en">Get encrypted object</para>
    ///     <para xml:lang="zh">获取加密对象</para>
    /// </summary>
    /// <param name="bucketName">
    ///     <para xml:lang="en">Bucket name</para>
    ///     <para xml:lang="zh">存储桶名称</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </param>
    /// <param name="range">
    ///     <para xml:lang="en">Byte range to download</para>
    ///     <para xml:lang="zh">要下载的字节范围</para>
    /// </param>
    Task<Stream> GetEncryptedObjectAsync(string bucketName, string key, string? range = null);
}

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

/// <summary>
///     <para xml:lang="en">Delete objects result</para>
///     <para xml:lang="zh">删除对象结果</para>
/// </summary>
public class DeleteObjectsResult
{
    /// <summary>
    ///     <para xml:lang="en">Deleted objects</para>
    ///     <para xml:lang="zh">已删除的对象</para>
    /// </summary>
    public List<DeletedObject> Deleted { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">Errors</para>
    ///     <para xml:lang="zh">错误</para>
    /// </summary>
    public List<DeleteError> Errors { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">Deleted object</para>
///     <para xml:lang="zh">已删除的对象</para>
/// </summary>
public class DeletedObject
{
    /// <summary>
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>
///     <para xml:lang="en">Delete error</para>
///     <para xml:lang="zh">删除错误</para>
/// </summary>
public class DeleteError
{
    /// <summary>
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Error code</para>
    ///     <para xml:lang="zh">错误代码</para>
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Error message</para>
    ///     <para xml:lang="zh">错误消息</para>
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

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

/// <summary>
///     <para xml:lang="en">Part ETag</para>
///     <para xml:lang="zh">部分ETag</para>
/// </summary>
public class PartETag
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

/// <summary>
///     <para xml:lang="en">Object version</para>
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
    ///     <para xml:lang="en">Is latest version</para>
    ///     <para xml:lang="zh">是否为最新版本</para>
    /// </summary>
    public bool IsLatest { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Is delete marker</para>
    ///     <para xml:lang="zh">是否为删除标记</para>
    /// </summary>
    public bool IsDeleteMarker { get; set; }
}

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

/// <summary>
///     <para xml:lang="en">Put encrypted object result</para>
///     <para xml:lang="zh">上传加密对象结果</para>
/// </summary>
public class PutEncryptedObjectResult
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

    /// <summary>
    ///     <para xml:lang="en">Encryption key ID</para>
    ///     <para xml:lang="zh">加密密钥ID</para>
    /// </summary>
    public string? EncryptionKeyId { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Server-side encryption</para>
    ///     <para xml:lang="zh">服务器端加密</para>
    /// </summary>
    public string? ServerSideEncryption { get; set; }
}

/// <summary>
///     <para xml:lang="en">Server-side encryption configuration</para>
///     <para xml:lang="zh">服务器端加密配置</para>
/// </summary>
public class ServerSideEncryptionConfiguration
{
    /// <summary>
    ///     <para xml:lang="en">Whether SSE is enabled</para>
    ///     <para xml:lang="zh">是否启用SSE</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Encryption algorithm</para>
    ///     <para xml:lang="zh">加密算法</para>
    /// </summary>
    public string Algorithm { get; set; } = "AES256";

    /// <summary>
    ///     <para xml:lang="en">KMS Key ID (for future use)</para>
    ///     <para xml:lang="zh">KMS密钥ID（未来使用）</para>
    /// </summary>
    public string? KmsKeyId { get; set; }
}
using EasilyNET.Mongo.AspNetCore.Abstraction;
using Microsoft.AspNetCore.Mvc;

namespace EasilyNET.Mongo.AspNetCore.Controllers;

/// <summary>
///     <para xml:lang="en">S3 Compatible API Controller for GridFS</para>
///     <para xml:lang="zh">GridFS的S3兼容API控制器</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Constructor</para>
///     <para xml:lang="zh">构造函数</para>
/// </remarks>
[ApiController]
[Route("s3/{bucketName}")]
public sealed class S3CompatibleController(IObjectStorage objectStorage) : ControllerBase
{

    /// <summary>
    ///     <para xml:lang="en">Put Object (S3 Compatible)</para>
    ///     <para xml:lang="zh">上传对象（S3兼容）</para>
    /// </summary>
    [HttpPut("{key}")]
    public async Task<IActionResult> PutObject(string bucketName, string key)
    {
        try
        {
            var contentType = Request.Headers.ContentType.ToString();
            var metadata = new Dictionary<string, string>();

            // Extract metadata from headers (x-amz-meta-*)
            foreach (var header in Request.Headers)
            {
                if (!header.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var metaKey = header.Key["x-amz-meta-".Length..];
                metadata[metaKey] = header.Value.ToString();
            }
            await objectStorage.PutObjectAsync(bucketName, key, Request.Body, contentType, metadata);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get Object (S3 Compatible)</para>
    ///     <para xml:lang="zh">下载对象（S3兼容）</para>
    /// </summary>
    [HttpGet("{key}")]
    public async Task<IActionResult> GetObject(string bucketName, string key)
    {
        try
        {
            // Check for Range header
            var rangeHeader = Request.Headers.Range.ToString();
            var stream = await objectStorage.GetObjectAsync(bucketName, key, rangeHeader);
            var metadata = await objectStorage.GetObjectMetadataAsync(bucketName, key);

            // Set common headers
            if (metadata.ETag != null)
            {
                Response.Headers.ETag = $"\"{metadata.ETag}\"";
            }
            Response.Headers.LastModified = metadata.LastModified.ToString("R");

            // Add custom metadata headers
            foreach (var kvp in metadata.Metadata)
            {
                Response.Headers[$"x-amz-meta-{kvp.Key}"] = kvp.Value;
            }

            // Handle range requests
            if (!string.IsNullOrEmpty(rangeHeader))
            {
                // Parse range to determine content length
                var rangeParts = rangeHeader.Replace("bytes=", "").Split('-');
                long start = 0;
                var end = metadata.ContentLength - 1;
                if (!string.IsNullOrEmpty(rangeParts[0]))
                {
                    start = long.Parse(rangeParts[0]);
                }
                if (!string.IsNullOrEmpty(rangeParts[1]))
                {
                    end = long.Parse(rangeParts[1]);
                }
                var contentLength = (end - start) + 1;
                Response.StatusCode = 206; // Partial Content
                Response.Headers.ContentRange = $"bytes {start}-{end}/{metadata.ContentLength}";
                Response.Headers.ContentLength = contentLength;
                Response.Headers.ContentType = metadata.ContentType ?? "application/octet-stream";
                Response.Headers.AcceptRanges = "bytes";
                return File(stream, metadata.ContentType ?? "application/octet-stream");
            }
            // Normal request
            Response.Headers.ContentType = metadata.ContentType ?? "application/octet-stream";
            Response.Headers.ContentLength = metadata.ContentLength;
            Response.Headers.AcceptRanges = "bytes";
            return File(stream, metadata.ContentType ?? "application/octet-stream");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "NoSuchKey", message = "The specified key does not exist." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Delete Object (S3 Compatible)</para>
    ///     <para xml:lang="zh">删除对象（S3兼容）</para>
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteObject(string bucketName, string key)
    {
        try
        {
            await objectStorage.DeleteObjectAsync(bucketName, key);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Head Object (S3 Compatible)</para>
    ///     <para xml:lang="zh">获取对象元数据（S3兼容）</para>
    /// </summary>
    [HttpHead("{key}")]
    public async Task<IActionResult> HeadObject(string bucketName, string key)
    {
        try
        {
            var metadata = await objectStorage.GetObjectMetadataAsync(bucketName, key);
            Response.Headers.ContentType = metadata.ContentType ?? "application/octet-stream";
            Response.Headers.ContentLength = metadata.ContentLength;
            if (metadata.ETag != null)
            {
                Response.Headers.ETag = $"\"{metadata.ETag}\"";
            }
            Response.Headers.LastModified = metadata.LastModified.ToString("R");

            // Add custom metadata headers
            foreach (var kvp in metadata.Metadata)
            {
                Response.Headers[$"x-amz-meta-{kvp.Key}"] = kvp.Value;
            }
            return Ok();
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "NoSuchKey", message = "The specified key does not exist." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Copy Object (S3 Compatible)</para>
    ///     <para xml:lang="zh">复制对象（S3兼容）</para>
    /// </summary>
    [HttpPut("copy/{key}")]
    public async Task<IActionResult> CopyObject(string bucketName, string key, [FromHeader(Name = "x-amz-copy-source")] string copySource)
    {
        try
        {
            if (string.IsNullOrEmpty(copySource))
            {
                return BadRequest(new { error = "MissingCopySource", message = "The copy source is not specified." });
            }

            // Parse copy source (format: /bucket/key)
            var sourceParts = copySource.TrimStart('/').Split('/', 2);
            if (sourceParts.Length != 2)
            {
                return BadRequest(new { error = "InvalidCopySource", message = "The copy source format is invalid." });
            }
            var sourceBucket = sourceParts[0];
            var sourceKey = sourceParts[1];

            // Extract metadata from headers
            var metadata = new Dictionary<string, string>();
            foreach (var header in Request.Headers)
            {
                if (!header.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var metaKey = header.Key["x-amz-meta-".Length..];
                metadata[metaKey] = header.Value.ToString();
            }
            await objectStorage.CopyObjectAsync(sourceBucket, sourceKey, bucketName, key, metadata);
            var sourceMetadata = await objectStorage.GetObjectMetadataAsync(sourceBucket, sourceKey);
            var result = new
            {
                CopyObjectResult = new
                {
                    ETag = $"\"{sourceMetadata.ETag}\"",
                    sourceMetadata.LastModified
                }
            };
            return Ok(result);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "NoSuchKey", message = "The specified key does not exist." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Delete Multiple Objects (S3 Compatible)</para>
    ///     <para xml:lang="zh">批量删除对象（S3兼容）</para>
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> DeleteObjects(string bucketName, [FromBody] DeleteObjectsRequest request)
    {
        try
        {
            var keys = request.Objects.Select(obj => obj.Key).ToList();
            var result = await objectStorage.DeleteObjectsAsync(bucketName, keys);
            var response = new
            {
                Deleted = result.Deleted.Select(d => new { d.Key }),
                Errors = result.Errors.Select(e => new
                {
                    e.Key,
                    e.Code,
                    e.Message
                })
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Initiate Multipart Upload (S3 Compatible)</para>
    ///     <para xml:lang="zh">初始化多部分上传（S3兼容）</para>
    /// </summary>
    [HttpPost("upload/{key}")]
    public async Task<IActionResult> InitiateMultipartUpload(string bucketName, string key, [FromQuery] string uploads)
    {
        try
        {
            if (uploads != "1")
            {
                return BadRequest(new { error = "InvalidRequest", message = "Invalid multipart upload request." });
            }
            var contentType = Request.Headers.ContentType.ToString();
            var metadata = new Dictionary<string, string>();

            // Extract metadata from headers
            foreach (var header in Request.Headers)
            {
                if (!header.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var metaKey = header.Key["x-amz-meta-".Length..];
                metadata[metaKey] = header.Value.ToString();
            }
            var result = await objectStorage.InitiateMultipartUploadAsync(bucketName, key, contentType, metadata);
            var response = new
            {
                Bucket = result.BucketName,
                result.Key,
                result.UploadId
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Upload Part (S3 Compatible)</para>
    ///     <para xml:lang="zh">上传部分（S3兼容）</para>
    /// </summary>
    [HttpPut("part/{key}")]
    public async Task<IActionResult> UploadPart(string bucketName, string key, [FromQuery] string uploadId, [FromQuery] int partNumber)
    {
        try
        {
            var result = await objectStorage.UploadPartAsync(bucketName, key, uploadId, partNumber, Request.Body);
            Response.Headers.ETag = $"\"{result.ETag}\"";
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Complete Multipart Upload (S3 Compatible)</para>
    ///     <para xml:lang="zh">完成多部分上传（S3兼容）</para>
    /// </summary>
    [HttpPost("complete/{key}")]
    public async Task<IActionResult> CompleteMultipartUpload(string bucketName, string key, [FromQuery] string uploadId, [FromBody] CompleteMultipartUploadRequest request)
    {
        try
        {
            var parts = request.Parts.Select(p => new PartETag { PartNumber = p.PartNumber, ETag = p.ETag.Trim('"') }).ToList();
            var result = await objectStorage.CompleteMultipartUploadAsync(bucketName, key, uploadId, parts);
            var response = new
            {
                Location = $"{bucketName}/{key}",
                Bucket = result.BucketName,
                result.Key,
                ETag = $"\"{result.ETag}\""
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Abort Multipart Upload (S3 Compatible)</para>
    ///     <para xml:lang="zh">中止多部分上传（S3兼容）</para>
    /// </summary>
    [HttpDelete("abort/{key}")]
    public async Task<IActionResult> AbortMultipartUpload(string bucketName, string key, [FromQuery] string uploadId)
    {
        try
        {
            await objectStorage.AbortMultipartUploadAsync(bucketName, key, uploadId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">List Objects V2 (S3 Compatible)</para>
    ///     <para xml:lang="zh">列出对象V2（S3兼容）</para>
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListObjectsV2(string bucketName,
        [FromQuery(Name = "list-type")]
        int? listType,
        [FromQuery]
        string? prefix,
        [FromQuery(Name = "continuation-token")]
        string? continuationToken,
        [FromQuery(Name = "max-keys")]
        int? maxKeys,
        [FromQuery(Name = "start-after")]
        string? startAfter)
    {
        try
        {
            // If list-type=2 is specified, use V2 API
            if (listType == 2)
            {
                var result = await objectStorage.ListObjectsV2Async(bucketName, prefix, continuationToken, maxKeys, startAfter);
                var response = new
                {
                    ListBucketResult = new
                    {
                        Name = result.BucketName,
                        result.Prefix,
                        result.ContinuationToken,
                        result.NextContinuationToken,
                        result.StartAfter,
                        KeyCount = result.Objects.Count,
                        result.MaxKeys,
                        result.IsTruncated,
                        Contents = result.Objects.Select(obj => new
                        {
                            obj.Key,
                            LastModified = obj.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            obj.ETag,
                            obj.Size,
                            obj.StorageClass
                        })
                    }
                };
                return Ok(response);
            }
            else
            {
                // Fall back to original ListObjects for backward compatibility
                var result = await objectStorage.ListObjectsAsync(bucketName, prefix, continuationToken, maxKeys);
                var response = new
                {
                    ListBucketResult = new
                    {
                        Name = result.BucketName,
                        result.Prefix,
                        result.Marker,
                        result.NextMarker,
                        result.MaxKeys,
                        result.IsTruncated,
                        Contents = result.Objects.Select(obj => new
                        {
                            obj.Key,
                            LastModified = obj.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            obj.ETag,
                            obj.Size,
                            obj.StorageClass
                        })
                    }
                };
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">List Objects (S3 Compatible)</para>
    ///     <para xml:lang="zh">列出对象（S3兼容）</para>
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> ListObjects(string bucketName,
        [FromQuery]
        string? prefix,
        [FromQuery]
        string? marker,
        [FromQuery(Name = "max-keys")]
        int? maxKeys)
    {
        try
        {
            var result = await objectStorage.ListObjectsAsync(bucketName, prefix, marker, maxKeys);
            var response = new
            {
                ListBucketResult = new
                {
                    Name = result.BucketName,
                    result.Prefix,
                    result.Marker,
                    result.NextMarker,
                    result.MaxKeys,
                    result.IsTruncated,
                    Contents = result.Objects.Select(obj => new
                    {
                        obj.Key,
                        LastModified = obj.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        obj.ETag,
                        obj.Size,
                        obj.StorageClass
                    })
                }
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Create Bucket (S3 Compatible)</para>
    ///     <para xml:lang="zh">创建存储桶（S3兼容）</para>
    /// </summary>
    [HttpPut("")]
    public async Task<IActionResult> CreateBucket(string bucketName)
    {
        try
        {
            await objectStorage.CreateBucketAsync(bucketName);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Delete Bucket (S3 Compatible)</para>
    ///     <para xml:lang="zh">删除存储桶（S3兼容）</para>
    /// </summary>
    [HttpDelete("")]
    public async Task<IActionResult> DeleteBucket(string bucketName)
    {
        try
        {
            await objectStorage.DeleteBucketAsync(bucketName);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">List Buckets (S3 Compatible)</para>
    ///     <para xml:lang="zh">列出存储桶（S3兼容）</para>
    /// </summary>
    [HttpGet("~/s3")]
    public async Task<IActionResult> ListBuckets()
    {
        try
        {
            var result = await objectStorage.ListBucketsAsync();
            var response = new
            {
                ListAllMyBucketsResult = new
                {
                    Buckets = new
                    {
                        Bucket = result.Buckets.Select(b => new
                        {
                            b.Name,
                            CreationDate = b.CreationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        })
                    }
                }
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Head Bucket (S3 Compatible)</para>
    ///     <para xml:lang="zh">检查存储桶（S3兼容）</para>
    /// </summary>
    [HttpHead("")]
    public async Task<IActionResult> HeadBucket(string bucketName)
    {
        try
        {
            var exists = await objectStorage.BucketExistsAsync(bucketName);
            if (exists)
            {
                return Ok();
            }
            return NotFound(new { error = "NoSuchBucket", message = "The specified bucket does not exist." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Put Encrypted Object (S3 Compatible)</para>
    ///     <para xml:lang="zh">上传加密对象（S3兼容）</para>
    /// </summary>
    [HttpPut("encrypted/{key}")]
    public async Task<IActionResult> PutEncryptedObject(string bucketName, string key)
    {
        try
        {
            var contentType = Request.Headers.ContentType.ToString();
            var metadata = new Dictionary<string, string>();

            // Extract metadata from headers (x-amz-meta-*)
            foreach (var header in Request.Headers)
            {
                if (!header.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var metaKey = header.Key["x-amz-meta-".Length..];
                metadata[metaKey] = header.Value.ToString();
            }

            // Check for server-side encryption headers
            var encryptionConfig = new ServerSideEncryptionConfiguration();
            var sseHeader = Request.Headers["x-amz-server-side-encryption"].ToString();
            if (!string.IsNullOrEmpty(sseHeader))
            {
                encryptionConfig.Enabled = true;
                encryptionConfig.Algorithm = sseHeader;
                encryptionConfig.KmsKeyId = Request.Headers["x-amz-server-side-encryption-aws-kms-key-id"].ToString();
            }

            var result = await objectStorage.PutEncryptedObjectAsync(bucketName, key, Request.Body, contentType, metadata, encryptionConfig);

            // Set encryption headers in response
            if (!string.IsNullOrEmpty(result.ServerSideEncryption))
            {
                Response.Headers["x-amz-server-side-encryption"] = result.ServerSideEncryption;
            }
            if (!string.IsNullOrEmpty(result.EncryptionKeyId))
            {
                Response.Headers["x-amz-server-side-encryption-aws-kms-key-id"] = result.EncryptionKeyId;
            }

            Response.Headers.ETag = $"\"{result.ETag}\"";
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get Encrypted Object (S3 Compatible)</para>
    ///     <para xml:lang="zh">获取加密对象（S3兼容）</para>
    /// </summary>
    [HttpGet("encrypted/{key}")]
    public async Task<IActionResult> GetEncryptedObject(string bucketName, string key)
    {
        try
        {
            // Check for Range header
            var rangeHeader = Request.Headers.Range.ToString();
            var stream = await objectStorage.GetEncryptedObjectAsync(bucketName, key, rangeHeader);
            var metadata = await objectStorage.GetObjectMetadataAsync(bucketName, key);

            // Set common headers
            if (metadata.ETag != null)
            {
                Response.Headers.ETag = $"\"{metadata.ETag}\"";
            }
            Response.Headers.LastModified = metadata.LastModified.ToString("R");

            // Add custom metadata headers
            foreach (var kvp in metadata.Metadata)
            {
                Response.Headers[$"x-amz-meta-{kvp.Key}"] = kvp.Value;
            }

            // Add encryption headers if present
            if (metadata.Metadata.TryGetValue("x-amz-server-side-encryption", out var sse))
            {
                Response.Headers["x-amz-server-side-encryption"] = sse;
            }
            if (metadata.Metadata.TryGetValue("x-amz-server-side-encryption-aws-kms-key-id", out var kmsKeyId))
            {
                Response.Headers["x-amz-server-side-encryption-aws-kms-key-id"] = kmsKeyId;
            }

            // Handle range requests
            if (!string.IsNullOrEmpty(rangeHeader))
            {
                // Parse range to determine content length
                var rangeParts = rangeHeader.Replace("bytes=", "").Split('-');
                long start = 0;
                var end = metadata.ContentLength - 1;
                if (!string.IsNullOrEmpty(rangeParts[0]))
                {
                    start = long.Parse(rangeParts[0]);
                }
                if (!string.IsNullOrEmpty(rangeParts[1]))
                {
                    end = long.Parse(rangeParts[1]);
                }
                var contentLength = (end - start) + 1;
                Response.StatusCode = 206; // Partial Content
                Response.Headers.ContentRange = $"bytes {start}-{end}/{metadata.ContentLength}";
                Response.Headers.ContentLength = contentLength;
                Response.Headers.ContentType = metadata.ContentType ?? "application/octet-stream";
                Response.Headers.AcceptRanges = "bytes";
                return File(stream, metadata.ContentType ?? "application/octet-stream");
            }
            // Normal request
            Response.Headers.ContentType = metadata.ContentType ?? "application/octet-stream";
            Response.Headers.ContentLength = metadata.ContentLength;
            Response.Headers.AcceptRanges = "bytes";
            return File(stream, metadata.ContentType ?? "application/octet-stream");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "NoSuchKey", message = "The specified key does not exist." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get Object Version (S3 Compatible)</para>
    ///     <para xml:lang="zh">获取对象版本（S3兼容）</para>
    /// </summary>
    [HttpGet("version/{key}")]
    public async Task<IActionResult> GetObjectVersion(string bucketName, string key, [FromQuery] string? versionId)
    {
        try
        {
            var version = await objectStorage.GetObjectVersionAsync(bucketName, key, versionId);
            var response = new
            {
                Version = new
                {
                    version.VersionId,
                    version.Key,
                    LastModified = version.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    version.Size,
                    version.ETag,
                    version.IsLatest,
                    version.IsDeleteMarker
                }
            };
            return Ok(response);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "NoSuchKey", message = "The specified key does not exist." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">List Object Versions (S3 Compatible)</para>
    ///     <para xml:lang="zh">列出对象版本（S3兼容）</para>
    /// </summary>
    [HttpGet("versions")]
    public async Task<IActionResult> ListObjectVersions(string bucketName, [FromQuery] string? prefix)
    {
        try
        {
            var result = await objectStorage.ListObjectVersionsAsync(bucketName, prefix);
            var response = new
            {
                ListVersionsResult = new
                {
                    Name = result.BucketName,
                    result.Prefix,
                    Versions = result.Versions.Select(v => new
                    {
                        v.VersionId,
                        v.Key,
                        LastModified = v.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        v.Size,
                        v.ETag,
                        v.IsLatest,
                        v.IsDeleteMarker
                    }),
                    DeleteMarkers = result.DeleteMarkers.Select(d => new
                    {
                        d.VersionId,
                        d.Key,
                        LastModified = d.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        d.IsLatest,
                        d.IsDeleteMarker
                    })
                }
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
///     <para xml:lang="en">Delete Objects Request</para>
///     <para xml:lang="zh">删除对象请求</para>
/// </summary>
public class DeleteObjectsRequest
{
    /// <summary>
    ///     <para xml:lang="en">Objects to delete</para>
    ///     <para xml:lang="zh">要删除的对象</para>
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<DeleteObject> Objects { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">Delete Object</para>
///     <para xml:lang="zh">删除对象</para>
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class DeleteObject
{
    /// <summary>
    ///     <para xml:lang="en">Object key</para>
    ///     <para xml:lang="zh">对象键</para>
    /// </summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>
///     <para xml:lang="en">Complete Multipart Upload Request</para>
///     <para xml:lang="zh">完成多部分上传请求</para>
/// </summary>
public class CompleteMultipartUploadRequest
{
    /// <summary>
    ///     <para xml:lang="en">Completed parts</para>
    ///     <para xml:lang="zh">已完成的部件</para>
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<CompletedPart> Parts { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">Completed Part</para>
///     <para xml:lang="zh">已完成的部件</para>
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class CompletedPart
{
    /// <summary>
    ///     <para xml:lang="en">Part number</para>
    ///     <para xml:lang="zh">部件编号</para>
    /// </summary>
    public int PartNumber { get; set; }

    /// <summary>
    ///     <para xml:lang="en">ETag</para>
    ///     <para xml:lang="zh">ETag</para>
    /// </summary>
    public string ETag { get; set; } = string.Empty;
}
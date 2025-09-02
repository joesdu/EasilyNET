using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using EasilyNET.Mongo.AspNetCore.Encryption;
using EasilyNET.Mongo.AspNetCore.Security;
using EasilyNET.Mongo.AspNetCore.Versioning;

namespace EasilyNET.Mongo.AspNetCore.Abstraction;

/// <summary>
///     <para xml:lang="en">GridFS object storage implementation</para>
///     <para xml:lang="zh">GridFS对象存储实现</para>
/// </summary>
public class GridFSObjectStorage : IObjectStorage
{
    private readonly IGridFSBucket _bucket;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, (ObjectMetadata Metadata, DateTime CacheTime)> _metadataCache = new();

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS存储桶</para>
    /// </param>
    public GridFSObjectStorage(IGridFSBucket bucket)
    {
        _bucket = bucket;
    }

    /// <inheritdoc />
    public async Task PutObjectAsync(string bucketName, string key, Stream stream, string? contentType = null, Dictionary<string, string>? metadata = null)
    {
        var options = new GridFSUploadOptions
        {
            Metadata = []
        };
        if (!string.IsNullOrEmpty(contentType))
        {
            options.Metadata["contentType"] = contentType;
        }
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                options.Metadata[kvp.Key] = kvp.Value;
            }
        }
        await _bucket.UploadFromStreamAsync(key, stream, options);

        // Invalidate cache
        _metadataCache.Remove(key);
    }

    /// <inheritdoc />
    public async Task<Stream> GetObjectAsync(string bucketName, string key, string? range = null)
    {
        var id = await GetObjectIdAsync(key);
        if (id == ObjectId.Empty)
        {
            throw new FileNotFoundException($"Object {key} not found");
        }

        // For range requests, we still need to load the full file and extract the range
        // For full file requests, return a streaming GridFS download stream
        if (!string.IsNullOrEmpty(range))
        {
            var memoryStream = new MemoryStream();
            await _bucket.DownloadToStreamAsync(id, memoryStream);
            memoryStream.Position = 0;
            var rangeStream = await ProcessRangeRequestAsync(memoryStream, range);
            return rangeStream;
        }
        // Return GridFS download stream directly for better memory efficiency
        var downloadStream = await _bucket.OpenDownloadStreamAsync(id);
        return downloadStream;
    }

    /// <inheritdoc />
    public async Task CopyObjectAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, Dictionary<string, string>? metadata = null)
    {
        var sourceId = await GetObjectIdAsync(sourceKey);
        if (sourceId == ObjectId.Empty)
        {
            throw new FileNotFoundException($"Source object {sourceKey} not found");
        }
        var memoryStream = new MemoryStream();
        await _bucket.DownloadToStreamAsync(sourceId, memoryStream);
        memoryStream.Position = 0;
        var sourceMetadata = await GetObjectMetadataAsync(sourceBucket, sourceKey);
        var contentType = sourceMetadata.ContentType;

        // Merge metadata
        var finalMetadata = new Dictionary<string, string>(sourceMetadata.Metadata);
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                finalMetadata[kvp.Key] = kvp.Value;
            }
        }
        await PutObjectAsync(destinationBucket, destinationKey, memoryStream, contentType, finalMetadata);
    }

    /// <inheritdoc />
    public async Task<DeleteObjectsResult> DeleteObjectsAsync(string bucketName, IEnumerable<string> keys)
    {
        var result = new DeleteObjectsResult();
        foreach (var key in keys)
        {
            try
            {
                var id = await GetObjectIdAsync(key);
                if (id != ObjectId.Empty)
                {
                    await _bucket.DeleteAsync(id);
                    result.Deleted.Add(new() { Key = key });
                }
                else
                {
                    result.Errors.Add(new()
                    {
                        Key = key,
                        Code = "NoSuchKey",
                        Message = "The specified key does not exist."
                    });
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new()
                {
                    Key = key,
                    Code = "InternalError",
                    Message = ex.Message
                });
            }
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<ListObjectsResult> ListObjectsAsync(string bucketName, string? prefix = null, string? marker = null, int? maxKeys = null)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Empty;
        if (!string.IsNullOrEmpty(prefix))
        {
            filter = Builders<GridFSFileInfo>.Filter.Regex(x => x.Filename, new($"^{Regex.Escape(prefix)}"));
        }
        var files = await (await _bucket.FindAsync(filter)).ToListAsync();
        var sortedFiles = files.OrderBy(f => f.Filename).ToList();

        // Apply pagination
        var startIndex = 0;
        if (!string.IsNullOrEmpty(marker))
        {
            var markerIndex = sortedFiles.FindIndex(f => f.Filename == marker);
            if (markerIndex >= 0)
            {
                startIndex = markerIndex + 1;
            }
        }
        var pageSize = maxKeys ?? 1000;
        var paginatedFiles = sortedFiles.Skip(startIndex).Take(pageSize).ToList();
        var result = new ListObjectsResult
        {
            BucketName = bucketName,
            Prefix = prefix,
            Marker = marker,
            MaxKeys = pageSize,
            IsTruncated = startIndex + pageSize < sortedFiles.Count
        };
        if (result.IsTruncated)
        {
            result.NextMarker = paginatedFiles.Last().Filename;
        }
        foreach (var file in paginatedFiles)
        {
            result.Objects.Add(new()
            {
                Key = file.Filename,
                LastModified = file.UploadDateTime,
                Size = file.Length,
                ETag = $"\"{file.Id}\"",
                StorageClass = "STANDARD"
            });
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<ListObjectsV2Result> ListObjectsV2Async(string bucketName, string? prefix = null, string? continuationToken = null, int? maxKeys = null, string? startAfter = null)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Empty;
        if (!string.IsNullOrEmpty(prefix))
        {
            filter = Builders<GridFSFileInfo>.Filter.Regex(x => x.Filename, new($"^{Regex.Escape(prefix)}"));
        }
        var files = await (await _bucket.FindAsync(filter)).ToListAsync();
        var sortedFiles = files.OrderBy(f => f.Filename).ToList();

        // Apply startAfter filter
        if (!string.IsNullOrEmpty(startAfter))
        {
            sortedFiles = sortedFiles.Where(f => string.Compare(f.Filename, startAfter, StringComparison.Ordinal) > 0).ToList();
        }

        // Apply continuation token (simplified as marker)
        var startIndex = 0;
        if (!string.IsNullOrEmpty(continuationToken))
        {
            var markerIndex = sortedFiles.FindIndex(f => f.Filename == continuationToken);
            if (markerIndex >= 0)
            {
                startIndex = markerIndex + 1;
            }
        }
        var pageSize = maxKeys ?? 1000;
        var paginatedFiles = sortedFiles.Skip(startIndex).Take(pageSize + 1).ToList(); // +1 to check if there are more
        var result = new ListObjectsV2Result
        {
            BucketName = bucketName,
            Prefix = prefix,
            ContinuationToken = continuationToken,
            StartAfter = startAfter,
            MaxKeys = pageSize,
            IsTruncated = paginatedFiles.Count > pageSize
        };

        // Set next continuation token if truncated
        if (result.IsTruncated)
        {
            result.NextContinuationToken = paginatedFiles[pageSize - 1].Filename;
            paginatedFiles = paginatedFiles.Take(pageSize).ToList();
        }
        foreach (var file in paginatedFiles)
        {
            result.Objects.Add(new()
            {
                Key = file.Filename,
                LastModified = file.UploadDateTime,
                Size = file.Length,
                ETag = $"\"{file.Id}\"",
                StorageClass = "STANDARD"
            });
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<InitiateMultipartUploadResult> InitiateMultipartUploadAsync(string bucketName, string key, string? contentType = null, Dictionary<string, string>? metadata = null)
    {
        // For GridFS, we'll simulate multipart upload by storing parts as separate files
        // and combining them when complete
        var uploadId = Guid.NewGuid().ToString();

        // Store upload metadata
        var uploadMetadata = new BsonDocument
        {
            ["uploadId"] = uploadId,
            ["key"] = key,
            ["bucketName"] = bucketName,
            ["contentType"] = contentType ?? "application/octet-stream",
            ["parts"] = new BsonArray(),
            ["created"] = DateTime.UtcNow
        };
        if (metadata != null)
        {
            uploadMetadata["metadata"] = new BsonDocument(metadata);
        }
        await _bucket.UploadFromBytesAsync($"__multipart__/{uploadId}/metadata", [],
            new() { Metadata = uploadMetadata });
        return new()
        {
            BucketName = bucketName,
            Key = key,
            UploadId = uploadId
        };
    }

    /// <inheritdoc />
    public async Task<UploadPartResult> UploadPartAsync(string bucketName, string key, string uploadId, int partNumber, Stream stream)
    {
        var partKey = $"__multipart__/{uploadId}/part-{partNumber}";
        var partData = new byte[stream.Length];
        var totalBytesRead = 0;
        while (totalBytesRead < stream.Length)
        {
            var bytesRead = await stream.ReadAsync(partData.AsMemory(totalBytesRead, (int)(stream.Length - totalBytesRead)));
            if (bytesRead == 0)
            {
                break;
            }
            totalBytesRead += bytesRead;
        }
        var partMetadata = new BsonDocument
        {
            ["uploadId"] = uploadId,
            ["partNumber"] = partNumber,
            ["size"] = partData.Length
        };
        await _bucket.UploadFromBytesAsync(partKey, partData, new() { Metadata = partMetadata });

        // Update upload metadata
        await UpdateMultipartUploadMetadataAsync(uploadId, partNumber, partData.Length);
        return new()
        {
            PartNumber = partNumber,
            ETag = $"\"{partKey.GetHashCode()}\""
        };
    }

    /// <inheritdoc />
    public async Task<CompleteMultipartUploadResult> CompleteMultipartUploadAsync(string bucketName, string key, string uploadId, IEnumerable<PartETag> parts)
    {
        // Get all parts
        var partsList = parts.OrderBy(p => p.PartNumber).ToList();

        // Use GridFS upload stream for better memory efficiency
        var uploadOptions = new GridFSUploadOptions
        {
            Metadata = []
        };

        // Get upload metadata for content type and custom metadata
        var metadataId = await GetObjectIdAsync($"__multipart__/{uploadId}/metadata");
        if (metadataId != ObjectId.Empty)
        {
            var metadataFile = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, metadataId))).FirstOrDefaultAsync();
            if (metadataFile?.Metadata != null)
            {
                if (metadataFile.Metadata.TryGetValue("contentType", out var contentType))
                {
                    uploadOptions.Metadata["contentType"] = contentType;
                }
                if (metadataFile.Metadata.TryGetValue("metadata", out var metaDoc) && metaDoc is BsonDocument metaBson)
                {
                    foreach (var element in metaBson)
                    {
                        uploadOptions.Metadata[element.Name] = element.Value;
                    }
                }
            }
        }

        // Upload combined file using streaming approach
        await using (var uploadStream = await _bucket.OpenUploadStreamAsync(key, uploadOptions))
        {
            foreach (var partKey in partsList.Select(part => $"__multipart__/{uploadId}/part-{part.PartNumber}"))
            {
                var partId = await GetObjectIdAsync(partKey);
                if (partId == ObjectId.Empty)
                {
                    continue;
                }
                await using var partStream = await _bucket.OpenDownloadStreamAsync(partId);
                await partStream.CopyToAsync(uploadStream);
            }
        }

        // Clean up multipart files
        await CleanupMultipartUploadAsync(uploadId);
        return new()
        {
            BucketName = bucketName,
            Key = key,
            ETag = $"\"{key.GetHashCode()}\""
        };
    }

    /// <inheritdoc />
    public async Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId)
    {
        await CleanupMultipartUploadAsync(uploadId);
    }

    /// <inheritdoc />
    public async Task DeleteObjectAsync(string bucketName, string key)
    {
        var id = await GetObjectIdAsync(key);
        if (id != ObjectId.Empty)
        {
            await _bucket.DeleteAsync(id);
        }

        // Invalidate cache
        _metadataCache.Remove(key);
    }

    /// <inheritdoc />
    public async Task<bool> ObjectExistsAsync(string bucketName, string key)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, key);
        var files = await (await _bucket.FindAsync(filter)).ToListAsync();
        return files.Count > 0;
    }

    /// <inheritdoc />
    public async Task<ObjectMetadata> GetObjectMetadataAsync(string bucketName, string key)
    {
        // Check cache first
        if (_metadataCache.TryGetValue(key, out var cached) &&
            DateTime.UtcNow - cached.CacheTime < _cacheDuration)
        {
            return cached.Metadata;
        }
        var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, key);
        var fileInfo = await (await _bucket.FindAsync(filter)).FirstOrDefaultAsync();
        if (fileInfo == null)
        {
            throw new FileNotFoundException($"Object {key} not found");
        }
        var metadata = new ObjectMetadata
        {
            ContentLength = fileInfo.Length,
            LastModified = fileInfo.UploadDateTime,
            ETag = fileInfo.Id.ToString()
        };
        if (fileInfo.Metadata != null)
        {
            if (fileInfo.Metadata.TryGetValue("contentType", out var contentType))
            {
                metadata.ContentType = contentType.AsString;
            }
            foreach (var element in fileInfo.Metadata)
            {
                if (element.Name != "contentType")
                {
                    metadata.Metadata[element.Name] = element.Value.AsString;
                }
            }
        }

        // Cache the metadata
        _metadataCache[key] = (metadata, DateTime.UtcNow);
        return metadata;
    }

    /// <inheritdoc />
    public async Task CreateBucketAsync(string bucketName)
    {
        // For GridFS, buckets are logical - we just track them in a special metadata file
        var bucketKey = $"__bucket__/{bucketName}";
        var bucketMetadata = new BsonDocument
        {
            ["bucketName"] = bucketName,
            ["created"] = DateTime.UtcNow
        };
        await _bucket.UploadFromBytesAsync(bucketKey, [],
            new() { Metadata = bucketMetadata });
    }

    /// <inheritdoc />
    public async Task DeleteBucketAsync(string bucketName)
    {
        // Delete all objects in the bucket first
        var filter = Builders<GridFSFileInfo>.Filter.Regex(x => x.Filename, new($"^{Regex.Escape(bucketName)}/"));
        var files = await (await _bucket.FindAsync(filter)).ToListAsync();
        foreach (var file in files)
        {
            await _bucket.DeleteAsync(file.Id);
        }

        // Delete bucket metadata
        var bucketKey = $"__bucket__/{bucketName}";
        var bucketId = await GetObjectIdAsync(bucketKey);
        if (bucketId != ObjectId.Empty)
        {
            await _bucket.DeleteAsync(bucketId);
        }
    }

    /// <inheritdoc />
    public async Task<ListBucketsResult> ListBucketsAsync()
    {
        var filter = Builders<GridFSFileInfo>.Filter.Regex(x => x.Filename, new("^__bucket__/"));
        var bucketFiles = await (await _bucket.FindAsync(filter)).ToListAsync();
        var result = new ListBucketsResult();
        foreach (var file in bucketFiles)
        {
            if (file.Metadata != null && file.Metadata.TryGetValue("bucketName", out var bucketName))
            {
                result.Buckets.Add(new()
                {
                    Name = bucketName.AsString,
                    CreationDate = file.UploadDateTime
                });
            }
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> BucketExistsAsync(string bucketName)
    {
        var bucketKey = $"__bucket__/{bucketName}";
        var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, bucketKey);
        var files = await (await _bucket.FindAsync(filter)).ToListAsync();
        return files.Count > 0;
    }

    /// <inheritdoc />
    public async Task<bool> CheckPermissionAsync(string bucketName, string key, string operation, string accessKey)
    {
        // In a real implementation, this would check against IAM policies
        // For now, we'll use a basic policy manager
        var policyManager = new S3IamPolicyManager();

        // Create a default admin policy for demo purposes
        var adminPolicy = S3IamPolicyManager.CreateAdminPolicy();
        policyManager.AddPolicy("AdminPolicy", adminPolicy);

        // Create a demo user with admin permissions
        policyManager.AddUser("demo-user", accessKey, "demo-secret", ["AdminPolicy"]);

        // Check permission using the policy manager
        await Task.Yield(); // Ensure async behavior
        return policyManager.HasPermission(accessKey, operation, $"arn:aws:s3:::{bucketName}/{key}", bucketName, key);
    }

    /// <inheritdoc />
    public async Task<ObjectVersion> GetObjectVersionAsync(string bucketName, string key, string? versionId = null)
    {
        // GridFS doesn't natively support versioning, so we'll simulate it using versioning manager
        var versioningManager = new S3ObjectVersioningManager();

        // Get version using the versioning manager
        var version = versioningManager.GetVersion(bucketName, key, versionId);
        if (version != null)
        {
            await Task.Yield(); // Ensure async behavior
            // Convert from Versioning.ObjectVersion to Abstraction.ObjectVersion
            return new()
            {
                VersionId = version.VersionId,
                Key = version.Key,
                LastModified = version.LastModified,
                Size = version.Size,
                ETag = version.ETag,
                IsLatest = version.IsLatest,
                IsDeleteMarker = false // Versioning manager doesn't support delete markers yet
            };
        }

        // Fallback to current version if no versioned data exists
        var metadata = await GetObjectMetadataAsync(bucketName, key);
        return new()
        {
            VersionId = versionId ?? "current",
            Key = key,
            LastModified = metadata.LastModified,
            Size = metadata.ContentLength,
            ETag = metadata.ETag,
            IsLatest = true,
            IsDeleteMarker = false
        };
    }

    /// <inheritdoc />
    public async Task<ListObjectVersionsResult> ListObjectVersionsAsync(string bucketName, string? prefix = null)
    {
        // Use versioning manager to list versions
        var versioningManager = new S3ObjectVersioningManager();
        var versions = versioningManager.ListVersions(bucketName, prefix);

        var result = new ListObjectVersionsResult
        {
            BucketName = bucketName,
            Prefix = prefix
        };

        // Convert from Versioning.ObjectVersion to Abstraction.ObjectVersion
        foreach (var version in versions)
        {
            result.Versions.Add(new()
            {
                VersionId = version.VersionId,
                Key = version.Key,
                LastModified = version.LastModified,
                Size = version.Size,
                ETag = version.ETag,
                IsLatest = version.IsLatest,
                IsDeleteMarker = false // Versioning manager doesn't support delete markers yet
            });
        }

        // If no versioned data exists, fall back to current versions
        if (result.Versions.Count == 0)
        {
            var listResult = await ListObjectsAsync(bucketName, prefix);
            foreach (var obj in listResult.Objects)
            {
                result.Versions.Add(new()
                {
                    VersionId = "current",
                    Key = obj.Key,
                    LastModified = obj.LastModified,
                    Size = obj.Size,
                    ETag = obj.ETag,
                    IsLatest = true,
                    IsDeleteMarker = false
                });
            }
        }

        await Task.Yield(); // Ensure async behavior
        return result;
    }

    /// <inheritdoc />
    public async Task<PutEncryptedObjectResult> PutEncryptedObjectAsync(string bucketName, string key, Stream stream, string? contentType = null, Dictionary<string, string>? metadata = null, ServerSideEncryptionConfiguration? encryptionConfig = null)
    {
        if (encryptionConfig?.Enabled == true)
        {
            // Get master key from environment or configuration
            var masterKey = Environment.GetEnvironmentVariable("EASILYNET_MASTER_KEY") ?? "DefaultMasterKey12345678901234567890123456789012";

            // Create encryption manager and encrypt the stream
            var encryptionManager = new S3ServerSideEncryptionManager(masterKey);

            // Encrypt the stream
            var (encryptedStream, keyId, encryptedKey) = await encryptionManager.EncryptAsync(stream, encryptionConfig.Algorithm);

            // Prepare metadata with encryption information
            var finalMetadata = metadata ?? new Dictionary<string, string>();
            finalMetadata["x-amz-server-side-encryption"] = encryptionConfig.Algorithm;
            finalMetadata["x-amz-server-side-encryption-key-id"] = keyId;
            finalMetadata["x-amz-server-side-encryption-key"] = encryptedKey;
            if (!string.IsNullOrEmpty(encryptionConfig.KmsKeyId))
            {
                finalMetadata["x-amz-server-side-encryption-aws-kms-key-id"] = encryptionConfig.KmsKeyId;
            }

            // Store the encrypted data
            encryptedStream.Position = 0;
            await PutObjectAsync(bucketName, key, encryptedStream, contentType, finalMetadata);

            return new()
            {
                BucketName = bucketName,
                Key = key,
                ETag = $"\"{key.GetHashCode()}\"",
                EncryptionKeyId = keyId,
                ServerSideEncryption = encryptionConfig.Algorithm
            };
        }

        // If encryption is not enabled, just put the object normally
        await PutObjectAsync(bucketName, key, stream, contentType, metadata);
        return new()
        {
            BucketName = bucketName,
            Key = key,
            ETag = $"\"{key.GetHashCode()}\""
        };
    }

    /// <inheritdoc />
    public async Task<Stream> GetEncryptedObjectAsync(string bucketName, string key, string? range = null)
    {
        // Get the object metadata first to check if it's encrypted
        var metadata = await GetObjectMetadataAsync(bucketName, key);
        if (metadata == null)
        {
            throw new FileNotFoundException($"Object {key} not found in bucket {bucketName}");
        }

        // Check if the object is encrypted
        var isEncrypted = metadata.Metadata.TryGetValue("x-amz-server-side-encryption", out var encryptionAlgorithm);
        if (!isEncrypted || string.IsNullOrEmpty(encryptionAlgorithm))
        {
            // Not encrypted, return the object directly
            return await GetObjectAsync(bucketName, key, range);
        }

        // Get encryption key ID from metadata
        if (!metadata.Metadata.TryGetValue("x-amz-server-side-encryption-key-id", out var keyId) || string.IsNullOrEmpty(keyId))
        {
            throw new InvalidOperationException("Encrypted object is missing encryption key ID");
        }

        // Get the encrypted stream
        var encryptedStream = await GetObjectAsync(bucketName, key, range);

        // Get master key from environment or configuration
        var masterKey = Environment.GetEnvironmentVariable("EASILYNET_MASTER_KEY") ?? "DefaultMasterKey12345678901234567890123456789012";

        // Create encryption manager and decrypt the stream
        var encryptionManager = new S3ServerSideEncryptionManager(masterKey);

        try
        {
            return await encryptionManager.DecryptAsync(encryptedStream, keyId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decrypt object {key}: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Update multipart upload metadata</para>
    ///     <para xml:lang="zh">更新多部分上传元数据</para>
    /// </summary>
    private async Task UpdateMultipartUploadMetadataAsync(string uploadId, int partNumber, long size)
    {
        var metadataId = await GetObjectIdAsync($"__multipart__/{uploadId}/metadata");
        if (metadataId != ObjectId.Empty)
        {
            // Get existing metadata
            var metadataFile = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, metadataId))).FirstOrDefaultAsync();
            if (metadataFile?.Metadata != null)
            {
                var metadata = metadataFile.Metadata;

                // Get or create parts array
                if (!metadata.TryGetValue("parts", out var partsBson) || partsBson is not BsonArray parts)
                {
                    parts = [];
                    metadata["parts"] = parts;
                }

                // Check if part already exists (update) or add new part (insert)
                var existingPart = parts.FirstOrDefault(p => p is BsonDocument doc && doc.TryGetValue("partNumber", out var pn) && pn.AsInt32 == partNumber);
                if (existingPart != null)
                {
                    // Update existing part
                    var partDoc = existingPart.AsBsonDocument;
                    partDoc["size"] = size;
                    partDoc["lastModified"] = DateTime.UtcNow;
                }
                else
                {
                    // Add new part
                    var newPart = new BsonDocument
                    {
                        ["partNumber"] = partNumber,
                        ["size"] = size,
                        ["uploaded"] = DateTime.UtcNow
                    };
                    parts.Add(newPart);
                }

                // Update total size
                long totalSize = 0;
                foreach (var part in parts)
                {
                    if (part is BsonDocument doc && doc.TryGetValue("size", out var partSize))
                    {
                        totalSize += partSize.AsInt64;
                    }
                }
                metadata["totalSize"] = totalSize;

                // Update metadata file
                await _bucket.DeleteAsync(metadataId);
                await _bucket.UploadFromBytesAsync($"__multipart__/{uploadId}/metadata", [],
                    new() { Metadata = metadata });
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Clean up multipart upload files</para>
    ///     <para xml:lang="zh">清理多部分上传文件</para>
    /// </summary>
    private async Task CleanupMultipartUploadAsync(string uploadId)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Regex(x => x.Filename, new($"^__multipart__/{Regex.Escape(uploadId)}"));
        var files = await (await _bucket.FindAsync(filter)).ToListAsync();
        foreach (var file in files)
        {
            await _bucket.DeleteAsync(file.Id);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get object ID by filename</para>
    ///     <para xml:lang="zh">根据文件名获取对象ID</para>
    /// </summary>
    private async Task<ObjectId> GetObjectIdAsync(string filename)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, filename);
        var fileInfo = await (await _bucket.FindAsync(filter)).FirstOrDefaultAsync();
        return fileInfo?.Id ?? ObjectId.Empty;
    }

    /// <summary>
    ///     <para xml:lang="en">Process range request</para>
    ///     <para xml:lang="zh">处理范围请求</para>
    /// </summary>
    private static async Task<Stream> ProcessRangeRequestAsync(MemoryStream fullStream, string range)
    {
        // Parse range header (format: bytes=start-end)
        var rangeHeader = range.Replace("bytes=", "");
        var rangeParts = rangeHeader.Split('-');
        if (rangeParts.Length != 2)
        {
            throw new ArgumentException("Invalid range format");
        }
        long start = 0;
        var end = fullStream.Length - 1;
        if (!string.IsNullOrEmpty(rangeParts[0]))
        {
            start = long.Parse(rangeParts[0]);
        }
        if (!string.IsNullOrEmpty(rangeParts[1]))
        {
            end = long.Parse(rangeParts[1]);
        }

        // Validate range
        if (start < 0 || end >= fullStream.Length || start > end)
        {
            throw new ArgumentException("Invalid range");
        }
        var length = (end - start) + 1;
        var rangeStream = new MemoryStream((int)length);
        fullStream.Position = start;
        await fullStream.CopyToAsync(rangeStream, (int)length);
        rangeStream.Position = 0;
        return rangeStream;
    }
}
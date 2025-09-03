// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.Versioning;

/// <summary>
///     <para xml:lang="en">S3 Object Versioning Manager</para>
///     <para xml:lang="zh">S3对象版本控制管理器</para>
/// </summary>
public class S3ObjectVersioningManager
{
    private readonly Dictionary<string, bool> _bucketVersioningEnabled = [];
    private readonly Dictionary<string, List<ObjectVersion>> _objectVersions = [];

    /// <summary>
    ///     <para xml:lang="en">Enable versioning for bucket</para>
    ///     <para xml:lang="zh">为存储桶启用版本控制</para>
    /// </summary>
    public void EnableVersioning(string bucketName)
    {
        _bucketVersioningEnabled[bucketName] = true;
    }

    /// <summary>
    ///     <para xml:lang="en">Disable versioning for bucket</para>
    ///     <para xml:lang="zh">为存储桶禁用版本控制</para>
    /// </summary>
    public void DisableVersioning(string bucketName)
    {
        _bucketVersioningEnabled[bucketName] = false;
    }

    /// <summary>
    ///     <para xml:lang="en">Check if versioning is enabled for bucket</para>
    ///     <para xml:lang="zh">检查存储桶是否启用了版本控制</para>
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool IsVersioningEnabled(string bucketName) => _bucketVersioningEnabled.GetValueOrDefault(bucketName, false);

    /// <summary>
    ///     <para xml:lang="en">Create new version of object</para>
    ///     <para xml:lang="zh">创建对象的新版本</para>
    /// </summary>
    public ObjectVersion CreateVersion(string bucketName, string key, Stream content, string? contentType = null, Dictionary<string, string>? metadata = null)
    {
        var objectKey = $"{bucketName}/{key}";
        var versionId = GenerateVersionId();
        var version = new ObjectVersion
        {
            VersionId = versionId,
            BucketName = bucketName,
            Key = key,
            ContentType = contentType,
            Metadata = metadata ?? new Dictionary<string, string>(),
            Size = content.Length,
            LastModified = DateTime.UtcNow,
            IsLatest = true
        };

        // Mark previous versions as not latest
        if (_objectVersions.TryGetValue(objectKey, out var versions))
        {
            foreach (var v in versions)
            {
                v.IsLatest = false;
            }
        }
        else
        {
            _objectVersions[objectKey] = [];
        }
        _objectVersions[objectKey].Add(version);
        return version;
    }

    /// <summary>
    ///     <para xml:lang="en">Get object version</para>
    ///     <para xml:lang="zh">获取对象版本</para>
    /// </summary>
    public ObjectVersion? GetVersion(string bucketName, string key, string? versionId = null)
    {
        var objectKey = $"{bucketName}/{key}";
        if (!_objectVersions.TryGetValue(objectKey, out var versions))
        {
            return null;
        }
        return string.IsNullOrEmpty(versionId)
                   // Return latest version
                   ? versions.FirstOrDefault(v => v.IsLatest)
                   : versions.FirstOrDefault(v => v.VersionId == versionId);
    }

    /// <summary>
    ///     <para xml:lang="en">List object versions</para>
    ///     <para xml:lang="zh">列出对象版本</para>
    /// </summary>
    public List<ObjectVersion> ListVersions(string bucketName, string? prefix = null, string? keyMarker = null, string? versionIdMarker = null, int maxKeys = 1000)
    {
        var result = new List<ObjectVersion>();
        foreach (var kvp in _objectVersions)
        {
            var objectKey = kvp.Key;
            var versions = kvp.Value;

            // Parse bucket and key from objectKey
            var parts = objectKey.Split('/', 2);
            if (parts.Length != 2 || parts[0] != bucketName)
            {
                continue;
            }
            var key = parts[1];

            // Apply prefix filter
            if (!string.IsNullOrEmpty(prefix) && !key.StartsWith(prefix))
            {
                continue;
            }

            // Apply pagination
            var startCollecting = string.IsNullOrEmpty(keyMarker);
            if (!startCollecting && string.Compare(key, keyMarker, StringComparison.Ordinal) <= 0)
            {
                // Check version marker for same key
                if (key == keyMarker && !string.IsNullOrEmpty(versionIdMarker))
                {
                    var versionIndex = versions.FindIndex(v => v.VersionId == versionIdMarker);
                    if (versionIndex >= 0)
                    {
                        startCollecting = true;
                        versions = versions.Skip(versionIndex + 1).ToList();
                    }
                }
            }
            else if (string.Compare(key, keyMarker ?? "", StringComparison.Ordinal) > 0)
            {
                startCollecting = true;
            }
            if (startCollecting)
            {
                result.AddRange(versions);
            }
            if (result.Count >= maxKeys)
            {
                break;
            }
        }
        return result.Take(maxKeys).ToList();
    }

    /// <summary>
    ///     <para xml:lang="en">Delete object version</para>
    ///     <para xml:lang="zh">删除对象版本</para>
    /// </summary>
    public bool DeleteVersion(string bucketName, string key, string? versionId = null)
    {
        var objectKey = $"{bucketName}/{key}";
        if (!_objectVersions.TryGetValue(objectKey, out var versions))
        {
            return false;
        }
        // Delete latest version
        var versionToDelete = string.IsNullOrEmpty(versionId) ? versions.FirstOrDefault(v => v.IsLatest) : versions.FirstOrDefault(v => v.VersionId == versionId);
        if (versionToDelete == null)
        {
            return false;
        }
        versions.Remove(versionToDelete);

        // If we deleted the latest version, mark the next latest as latest
        if (versionToDelete.IsLatest && versions.Count > 0)
        {
            var newLatest = versions.OrderByDescending(v => v.LastModified).First();
            newLatest.IsLatest = true;
        }

        // If no versions left, remove the object entry
        if (versions.Count == 0)
        {
            _objectVersions.Remove(objectKey);
        }
        return true;
    }

    /// <summary>
    ///     <para xml:lang="en">Get versioning status</para>
    ///     <para xml:lang="zh">获取版本控制状态</para>
    /// </summary>
    public string GetVersioningStatus(string bucketName) => IsVersioningEnabled(bucketName) ? "Enabled" : "Suspended";

    private static string GenerateVersionId() =>
        // Generate a unique version ID (simplified)
        Guid.NewGuid().ToString("N")[..16].ToUpper();
}
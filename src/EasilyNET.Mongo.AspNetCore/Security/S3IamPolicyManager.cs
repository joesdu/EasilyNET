// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.Security;

/// <summary>
///     <para xml:lang="en">IAM Policy Manager for S3-compatible access control</para>
///     <para xml:lang="zh">S3兼容访问控制的IAM策略管理器</para>
/// </summary>
public class S3IamPolicyManager
{
    private readonly Dictionary<string, IamPolicy> _policies = [];
    private readonly Dictionary<string, List<string>> _userPolicies = [];
    private readonly Dictionary<string, IamUser> _users = [];

    /// <summary>
    ///     <para xml:lang="en">Add or update user</para>
    ///     <para xml:lang="zh">添加或更新用户</para>
    /// </summary>
    public void AddUser(string userId, string accessKey, string secretKey, List<string>? policies = null)
    {
        _users[userId] = new()
        {
            UserId = userId,
            AccessKey = accessKey,
            SecretKey = secretKey,
            Policies = policies ?? []
        };
        if (policies != null)
        {
            _userPolicies[userId] = policies;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Add policy</para>
    ///     <para xml:lang="zh">添加策略</para>
    /// </summary>
    public void AddPolicy(string policyName, IamPolicy policy)
    {
        _policies[policyName] = policy;
    }

    /// <summary>
    ///     <para xml:lang="en">Check if user has permission for action</para>
    ///     <para xml:lang="zh">检查用户是否有执行操作的权限</para>
    /// </summary>
    public bool HasPermission(string accessKey, string action, string resource, string? bucket = null, string? key = null)
    {
        var user = _users.Values.FirstOrDefault(u => u.AccessKey == accessKey);
        if (user == null)
        {
            return false;
        }
        var userPolicies = _userPolicies.GetValueOrDefault(user.UserId, []);
        foreach (var policyName in userPolicies)
        {
            if (!_policies.TryGetValue(policyName, out var policy))
            {
                continue;
            }
            if (EvaluatePolicy(policy, action, resource, bucket, key))
            {
                return true;
            }
        }
        return false;
    }

    private static bool EvaluatePolicy(IamPolicy policy, string action, string resource, string? bucket, string? key)
    {
        foreach (var statement in from statement in policy.Statement
                                  where statement.Effect == "Allow"
                                  where statement.Action.Contains("*") || statement.Action.Contains(action)
                                  where statement.Resource.Contains("*") || statement.Resource.Contains(resource)
                                  select statement)
        {
            // Check bucket-specific conditions
            if (bucket == null || !statement.Condition.TryGetValue("StringEquals", out var conditions))
            {
                return true;
            }
            if (!conditions.TryGetValue("s3:prefix", out var prefix) || key == null)
            {
                return true;
            }
            if (!key.StartsWith(prefix.ToString() ?? string.Empty))
            {
                continue;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Create default admin policy</para>
    ///     <para xml:lang="zh">创建默认管理员策略</para>
    /// </summary>
    public static IamPolicy CreateAdminPolicy() =>
        new()
        {
            Version = "2012-10-17",
            Statement =
            [
                new()
                {
                    Effect = "Allow",
                    Action = ["s3:*"],
                    Resource = ["arn:aws:s3:::*"]
                }
            ]
        };

    /// <summary>
    ///     <para xml:lang="en">Create read-only policy</para>
    ///     <para xml:lang="zh">创建只读策略</para>
    /// </summary>
    public static IamPolicy CreateReadOnlyPolicy() =>
        new()
        {
            Version = "2012-10-17",
            Statement =
            [
                new()
                {
                    Effect = "Allow",
                    Action = ["s3:GetObject", "s3:ListBucket"],
                    Resource = ["arn:aws:s3:::*"]
                }
            ]
        };

    /// <summary>
    ///     <para xml:lang="en">Create bucket-specific policy</para>
    ///     <para xml:lang="zh">创建存储桶特定策略</para>
    /// </summary>
    public static IamPolicy CreateBucketPolicy(string bucketName, List<string> allowedActions) =>
        new()
        {
            Version = "2012-10-17",
            Statement =
            [
                new()
                {
                    Effect = "Allow",
                    Action = allowedActions,
                    Resource = [$"arn:aws:s3:::{bucketName}", $"arn:aws:s3:::{bucketName}/*"]
                }
            ]
        };
}

/// <summary>
///     <para xml:lang="en">IAM User</para>
///     <para xml:lang="zh">IAM用户</para>
/// </summary>
public class IamUser
{
    /// <summary>
    ///     <para xml:lang="en">User ID</para>
    ///     <para xml:lang="zh">用户ID</para>
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Access Key</para>
    ///     <para xml:lang="zh">访问密钥</para>
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Secret Key</para>
    ///     <para xml:lang="zh">秘密密钥</para>
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Associated policies</para>
    ///     <para xml:lang="zh">关联的策略</para>
    /// </summary>
    public List<string> Policies { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">IAM Policy</para>
///     <para xml:lang="zh">IAM策略</para>
/// </summary>
public class IamPolicy
{
    /// <summary>
    ///     <para xml:lang="en">Policy version</para>
    ///     <para xml:lang="zh">策略版本</para>
    /// </summary>
    public string Version { get; set; } = "2012-10-17";

    /// <summary>
    ///     <para xml:lang="en">Policy statements</para>
    ///     <para xml:lang="zh">策略声明</para>
    /// </summary>
    public List<IamStatement> Statement { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">IAM Policy Statement</para>
///     <para xml:lang="zh">IAM策略声明</para>
/// </summary>
public class IamStatement
{
    /// <summary>
    ///     <para xml:lang="en">Effect (Allow/Deny)</para>
    ///     <para xml:lang="zh">效果（允许/拒绝）</para>
    /// </summary>
    public string Effect { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Actions</para>
    ///     <para xml:lang="zh">操作</para>
    /// </summary>
    public List<string> Action { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">Resources</para>
    ///     <para xml:lang="zh">资源</para>
    /// </summary>
    public List<string> Resource { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">Conditions</para>
    ///     <para xml:lang="zh">条件</para>
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public Dictionary<string, Dictionary<string, object>> Condition { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">S3 Action Types</para>
///     <para xml:lang="zh">S3操作类型</para>
/// </summary>
// ReSharper disable once UnusedType.Global
public static class S3Actions
{
    // Object operations
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
    public const string GetObject = "s3:GetObject";
    public const string PutObject = "s3:PutObject";
    public const string DeleteObject = "s3:DeleteObject";
    public const string CopyObject = "s3:CopyObject";
    public const string HeadObject = "s3:HeadObject";

    // Bucket operations
    public const string ListBucket = "s3:ListBucket";
    public const string CreateBucket = "s3:CreateBucket";
    public const string DeleteBucket = "s3:DeleteBucket";
    public const string HeadBucket = "s3:HeadBucket";

    // Multipart upload operations
    public const string InitiateMultipartUpload = "s3:InitiateMultipartUpload";
    public const string UploadPart = "s3:UploadPart";
    public const string CompleteMultipartUpload = "s3:CompleteMultipartUpload";
    public const string AbortMultipartUpload = "s3:AbortMultipartUpload";

    // Batch operations
    public const string DeleteObjects = "s3:DeleteObjects";

    // All operations
    public const string All = "s3:*";
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
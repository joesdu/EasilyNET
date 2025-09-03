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
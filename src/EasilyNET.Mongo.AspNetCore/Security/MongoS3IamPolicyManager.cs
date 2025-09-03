using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.Security;

/// <summary>
///     <para xml:lang="en">MongoDB-based IAM Policy Manager for S3-compatible access control</para>
///     <para xml:lang="zh">基于MongoDB的S3兼容访问控制IAM策略管理器</para>
/// </summary>
public class MongoS3IamPolicyManager
{
    private readonly IMongoCollection<AccessKeyDocument> _accessKeysCollection;
    private readonly IMongoCollection<IamPolicyDocument> _policiesCollection;
    private readonly IMongoCollection<IamUserDocument> _usersCollection;

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    public MongoS3IamPolicyManager(IMongoDatabase database)
    {
        _usersCollection = database.GetCollection<IamUserDocument>("iam_users");
        _policiesCollection = database.GetCollection<IamPolicyDocument>("iam_policies");
        _accessKeysCollection = database.GetCollection<AccessKeyDocument>("access_keys");

        // Create indexes
        CreateIndexesAsync().GetAwaiter().GetResult();
    }

    private async Task CreateIndexesAsync()
    {
        // User indexes
        await _usersCollection.Indexes.CreateOneAsync(new CreateIndexModel<IamUserDocument>(Builders<IamUserDocument>.IndexKeys.Ascending(u => u.UserId),
            new() { Unique = true }));
        await _usersCollection.Indexes.CreateOneAsync(new CreateIndexModel<IamUserDocument>(Builders<IamUserDocument>.IndexKeys.Ascending(u => u.AccessKey),
            new() { Unique = true }));

        // Policy indexes
        await _policiesCollection.Indexes.CreateOneAsync(new CreateIndexModel<IamPolicyDocument>(Builders<IamPolicyDocument>.IndexKeys.Ascending(p => p.PolicyName),
            new() { Unique = true }));

        // Access key indexes
        await _accessKeysCollection.Indexes.CreateOneAsync(new CreateIndexModel<AccessKeyDocument>(Builders<AccessKeyDocument>.IndexKeys.Ascending(k => k.AccessKeyId),
            new() { Unique = true }));
        await _accessKeysCollection.Indexes.CreateOneAsync(new CreateIndexModel<AccessKeyDocument>(Builders<AccessKeyDocument>.IndexKeys.Ascending(k => k.UserId)));
    }

    /// <summary>
    ///     <para xml:lang="en">Add or update user</para>
    ///     <para xml:lang="zh">添加或更新用户</para>
    /// </summary>
    public async Task AddUserAsync(string userId, string userName, string accessKey, string secretKey, List<string>? policies = null)
    {
        var userDoc = new IamUserDocument
        {
            UserId = userId,
            UserName = userName,
            AccessKey = accessKey,
            SecretKey = secretKey,
            Policies = policies ?? [],
            LastModified = DateTime.UtcNow
        };
        await _usersCollection.ReplaceOneAsync(Builders<IamUserDocument>.Filter.Eq(u => u.UserId, userId),
            userDoc,
            new ReplaceOptions { IsUpsert = true });
    }

    /// <summary>
    ///     <para xml:lang="en">Add policy</para>
    ///     <para xml:lang="zh">添加策略</para>
    /// </summary>
    public async Task AddPolicyAsync(string policyName, IamPolicy policy)
    {
        var policyDoc = new IamPolicyDocument
        {
            PolicyName = policyName,
            Version = policy.Version,
            Statements = policy.Statement,
            LastModified = DateTime.UtcNow
        };
        await _policiesCollection.ReplaceOneAsync(Builders<IamPolicyDocument>.Filter.Eq(p => p.PolicyName, policyName),
            policyDoc,
            new ReplaceOptions { IsUpsert = true });
    }

    /// <summary>
    ///     <para xml:lang="en">Get user by access key</para>
    ///     <para xml:lang="zh">通过访问密钥获取用户</para>
    /// </summary>
    public async Task<IamUserDocument?> GetUserByAccessKeyAsync(string accessKey)
    {
        return await _usersCollection.Find(u => u.AccessKey == accessKey && u.IsEnabled).FirstOrDefaultAsync();
    }

    /// <summary>
    ///     <para xml:lang="en">Get policy by name</para>
    ///     <para xml:lang="zh">通过名称获取策略</para>
    /// </summary>
    public async Task<IamPolicyDocument?> GetPolicyAsync(string policyName)
    {
        return await _policiesCollection.Find(p => p.PolicyName == policyName && p.IsEnabled).FirstOrDefaultAsync();
    }

    /// <summary>
    ///     <para xml:lang="en">Check if user has permission for action</para>
    ///     <para xml:lang="zh">检查用户是否有执行操作的权限</para>
    /// </summary>
    public async Task<bool> HasPermissionAsync(string accessKey, string action, string resource, string? bucket = null, string? key = null)
    {
        var user = await GetUserByAccessKeyAsync(accessKey);
        if (user == null)
        {
            return false;
        }
        foreach (var policyName in user.Policies)
        {
            var policy = await GetPolicyAsync(policyName);
            if (policy == null)
            {
                continue;
            }
            if (EvaluatePolicy(policy.ToIamPolicy(), action, resource, bucket, key))
            {
                return true;
            }
        }
        return false;
    }

    private static bool EvaluatePolicy(IamPolicy policy, string action, string resource, string? bucket, string? key)
    {
        foreach (var statement in policy.Statement.Where(s => s.Effect == "Allow"))
        {
            if (statement.Action.Contains("*") || statement.Action.Contains(action))
            {
                if (statement.Resource.Contains("*") || statement.Resource.Contains(resource))
                {
                    // Check bucket-specific conditions
                    if (bucket != null && statement.Condition.TryGetValue("StringEquals", out var conditions))
                    {
                        if (conditions.TryGetValue("s3:prefix", out var prefix) && key != null)
                        {
                            if (!key.StartsWith(prefix.ToString() ?? string.Empty))
                            {
                                continue;
                            }
                        }
                    }
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Get all users</para>
    ///     <para xml:lang="zh">获取所有用户</para>
    /// </summary>
    public async Task<List<IamUserDocument>> GetAllUsersAsync()
    {
        return await _usersCollection.Find(u => u.IsEnabled).ToListAsync();
    }

    /// <summary>
    ///     <para xml:lang="en">Get all policies</para>
    ///     <para xml:lang="zh">获取所有策略</para>
    /// </summary>
    public async Task<List<IamPolicyDocument>> GetAllPoliciesAsync()
    {
        return await _policiesCollection.Find(p => p.IsEnabled).ToListAsync();
    }

    /// <summary>
    ///     <para xml:lang="en">Get all access keys</para>
    ///     <para xml:lang="zh">获取所有访问密钥</para>
    /// </summary>
    public async Task<List<AccessKeyDocument>> GetAllAccessKeysAsync()
    {
        return await _accessKeysCollection.Find(k => k.IsEnabled).ToListAsync();
    }

    /// <summary>
    ///     <para xml:lang="en">Delete user</para>
    ///     <para xml:lang="zh">删除用户</para>
    /// </summary>
    public async Task DeleteUserAsync(string userId)
    {
        await _usersCollection.UpdateOneAsync(Builders<IamUserDocument>.Filter.Eq(u => u.UserId, userId),
            Builders<IamUserDocument>.Update.Set(u => u.IsEnabled, false).Set(u => u.LastModified, DateTime.UtcNow));
    }

    /// <summary>
    ///     <para xml:lang="en">Delete policy</para>
    ///     <para xml:lang="zh">删除策略</para>
    /// </summary>
    public async Task DeletePolicyAsync(string policyName)
    {
        await _policiesCollection.UpdateOneAsync(Builders<IamPolicyDocument>.Filter.Eq(p => p.PolicyName, policyName),
            Builders<IamPolicyDocument>.Update.Set(p => p.IsEnabled, false).Set(p => p.LastModified, DateTime.UtcNow));
    }

    /// <summary>
    ///     <para xml:lang="en">Update last used time for access key</para>
    ///     <para xml:lang="zh">更新访问密钥的最后使用时间</para>
    /// </summary>
    public async Task UpdateAccessKeyLastUsedAsync(string accessKeyId)
    {
        await _accessKeysCollection.UpdateOneAsync(Builders<AccessKeyDocument>.Filter.Eq(k => k.AccessKeyId, accessKeyId),
            Builders<AccessKeyDocument>.Update.Set(k => k.LastUsed, DateTime.UtcNow));
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
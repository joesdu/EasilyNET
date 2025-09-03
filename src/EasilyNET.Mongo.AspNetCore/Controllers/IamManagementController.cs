using EasilyNET.Mongo.AspNetCore.Security;
using Microsoft.AspNetCore.Mvc;

namespace EasilyNET.Mongo.AspNetCore.Controllers;

/// <summary>
///     <para xml:lang="en">IAM Management API Controller</para>
///     <para xml:lang="zh">IAM管理API控制器</para>
/// </summary>
[ApiController]
[Route("api/iam")]
public class IamManagementController(MongoS3IamPolicyManager iamManager) : ControllerBase
{
    #region User Management

    /// <summary>
    ///     <para xml:lang="en">Create a new user</para>
    ///     <para xml:lang="zh">创建新用户</para>
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Generate access keys
            var (accessKeyId, secretAccessKey) = GenerateAccessKeys();
            await iamManager.AddUserAsync(request.UserId,
                request.UserName,
                accessKeyId,
                secretAccessKey,
                request.Policies);
            return Ok(new
            {
                request.UserId,
                request.UserName,
                AccessKeyId = accessKeyId,
                SecretAccessKey = secretAccessKey,
                request.Policies
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get all users</para>
    ///     <para xml:lang="zh">获取所有用户</para>
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await iamManager.GetAllUsersAsync();
            return Ok(users.Select(u => new
            {
                u.UserId,
                u.UserName,
                u.AccessKey,
                u.Policies,
                u.CreatedAt,
                u.LastModified,
                u.IsEnabled
            }));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get user by ID</para>
    ///     <para xml:lang="zh">通过ID获取用户</para>
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        try
        {
            var users = await iamManager.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }
            return Ok(new
            {
                user.UserId,
                user.UserName,
                user.AccessKey,
                user.Policies,
                user.CreatedAt,
                user.LastModified,
                user.IsEnabled
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Delete user</para>
    ///     <para xml:lang="zh">删除用户</para>
    /// </summary>
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            await iamManager.DeleteUserAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Policy Management

    /// <summary>
    ///     <para xml:lang="en">Create a new policy</para>
    ///     <para xml:lang="zh">创建新策略</para>
    /// </summary>
    [HttpPost("policies")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
    {
        try
        {
            var policy = new IamPolicy
            {
                Version = request.Version ?? "2012-10-17",
                Statement = request.Statements
            };
            await iamManager.AddPolicyAsync(request.PolicyName, policy);
            return Ok(new
            {
                request.PolicyName,
                policy.Version,
                Statements = policy.Statement
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get all policies</para>
    ///     <para xml:lang="zh">获取所有策略</para>
    /// </summary>
    [HttpGet("policies")]
    public async Task<IActionResult> GetPolicies()
    {
        try
        {
            var policies = await iamManager.GetAllPoliciesAsync();
            return Ok(policies.Select(p => new
            {
                p.PolicyName,
                p.Version,
                p.Statements,
                p.CreatedAt,
                p.LastModified,
                p.IsEnabled
            }));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get policy by name</para>
    ///     <para xml:lang="zh">通过名称获取策略</para>
    /// </summary>
    [HttpGet("policies/{policyName}")]
    public async Task<IActionResult> GetPolicy(string policyName)
    {
        try
        {
            var policy = await iamManager.GetPolicyAsync(policyName);
            if (policy == null)
            {
                return NotFound(new { error = "Policy not found" });
            }
            return Ok(new
            {
                policy.PolicyName,
                policy.Version,
                policy.Statements,
                policy.CreatedAt,
                policy.LastModified,
                policy.IsEnabled
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Delete policy</para>
    ///     <para xml:lang="zh">删除策略</para>
    /// </summary>
    [HttpDelete("policies/{policyName}")]
    public async Task<IActionResult> DeletePolicy(string policyName)
    {
        try
        {
            await iamManager.DeletePolicyAsync(policyName);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Create default admin policy</para>
    ///     <para xml:lang="zh">创建默认管理员策略</para>
    /// </summary>
    [HttpPost("policies/admin")]
    public async Task<IActionResult> CreateAdminPolicy()
    {
        try
        {
            var policy = MongoS3IamPolicyManager.CreateAdminPolicy();
            await iamManager.AddPolicyAsync("Admin", policy);
            return Ok(new
            {
                PolicyName = "Admin",
                policy.Version,
                Statements = policy.Statement
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Create read-only policy</para>
    ///     <para xml:lang="zh">创建只读策略</para>
    /// </summary>
    [HttpPost("policies/readonly")]
    public async Task<IActionResult> CreateReadOnlyPolicy()
    {
        try
        {
            var policy = MongoS3IamPolicyManager.CreateReadOnlyPolicy();
            await iamManager.AddPolicyAsync("ReadOnly", policy);
            return Ok(new
            {
                PolicyName = "ReadOnly",
                policy.Version,
                Statements = policy.Statement
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Access Key Management

    /// <summary>
    ///     <para xml:lang="en">Generate new access keys for user</para>
    ///     <para xml:lang="zh">为用户生成新的访问密钥</para>
    /// </summary>
    [HttpPost("users/{userId}/keys")]
    public async Task<IActionResult> GenerateAccessKeys(string userId)
    {
        try
        {
            var users = await iamManager.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Generate new keys
            var (accessKeyId, secretAccessKey) = GenerateAccessKeys();

            // Update user with new keys
            await iamManager.AddUserAsync(user.UserId,
                user.UserName,
                accessKeyId,
                secretAccessKey,
                user.Policies);
            return Ok(new
            {
                user.UserId,
                AccessKeyId = accessKeyId,
                SecretAccessKey = secretAccessKey
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get all access keys</para>
    ///     <para xml:lang="zh">获取所有访问密钥</para>
    /// </summary>
    [HttpGet("keys")]
    public async Task<IActionResult> GetAccessKeys()
    {
        try
        {
            var keys = await iamManager.GetAllAccessKeysAsync();
            return Ok(keys.Select(k => new
            {
                k.AccessKeyId,
                k.UserId,
                k.UserName,
                k.Status,
                k.CreatedAt,
                k.LastUsed,
                k.IsEnabled
            }));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Utility Methods

    private static (string AccessKeyId, string SecretAccessKey) GenerateAccessKeys()
    {
        // Generate Access Key ID (20 characters, starts with AKIA)
        var accessKeyId = "AKIA" + GenerateRandomString(16);

        // Generate Secret Access Key (40 characters)
        var secretAccessKey = GenerateRandomString(40);
        return (accessKeyId, secretAccessKey);
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new(Enumerable.Repeat(chars, length)
                             .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    #endregion
}

/// <summary>
///     <para xml:lang="en">Create User Request</para>
///     <para xml:lang="zh">创建用户请求</para>
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    ///     <para xml:lang="en">User ID</para>
    ///     <para xml:lang="zh">用户ID</para>
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">User name</para>
    ///     <para xml:lang="zh">用户名</para>
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Policies to attach</para>
    ///     <para xml:lang="zh">要附加的策略</para>
    /// </summary>
    public List<string> Policies { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">Create Policy Request</para>
///     <para xml:lang="zh">创建策略请求</para>
/// </summary>
public class CreatePolicyRequest
{
    /// <summary>
    ///     <para xml:lang="en">Policy name</para>
    ///     <para xml:lang="zh">策略名称</para>
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Policy version</para>
    ///     <para xml:lang="zh">策略版本</para>
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Policy statements</para>
    ///     <para xml:lang="zh">策略声明</para>
    /// </summary>
    public List<IamStatement> Statements { get; set; } = [];
}
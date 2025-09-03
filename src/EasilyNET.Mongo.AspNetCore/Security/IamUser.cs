namespace EasilyNET.Mongo.AspNetCore.Security;

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
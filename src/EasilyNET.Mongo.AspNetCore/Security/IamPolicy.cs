namespace EasilyNET.Mongo.AspNetCore.Security;

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
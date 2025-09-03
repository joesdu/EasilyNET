namespace EasilyNET.Mongo.GridFS.S3.Security;

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
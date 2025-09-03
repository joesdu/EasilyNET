using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EasilyNET.Mongo.GridFS.S3.Security;

/// <summary>
///     <para xml:lang="en">MongoDB document for IAM Policy</para>
///     <para xml:lang="zh">IAM策略的MongoDB文档</para>
/// </summary>
[BsonIgnoreExtraElements]
public class IamPolicyDocument
{
    /// <summary>
    ///     <para xml:lang="en">Document ID</para>
    ///     <para xml:lang="zh">文档ID</para>
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    ///     <para xml:lang="en">Policy name</para>
    ///     <para xml:lang="zh">策略名称</para>
    /// </summary>
    [BsonElement("policyName")]
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Policy version</para>
    ///     <para xml:lang="zh">策略版本</para>
    /// </summary>
    [BsonElement("version")]
    public string Version { get; set; } = "2012-10-17";

    /// <summary>
    ///     <para xml:lang="en">Policy statements</para>
    ///     <para xml:lang="zh">策略声明</para>
    /// </summary>
    [BsonElement("statements")]
    public List<IamStatement> Statements { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">Creation time</para>
    ///     <para xml:lang="zh">创建时间</para>
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     <para xml:lang="en">Last modified time</para>
    ///     <para xml:lang="zh">最后修改时间</para>
    /// </summary>
    [BsonElement("lastModified")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     <para xml:lang="en">Is enabled</para>
    ///     <para xml:lang="zh">是否启用</para>
    /// </summary>
    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Convert to IamPolicy</para>
    ///     <para xml:lang="zh">转换为IamPolicy</para>
    /// </summary>
    public IamPolicy ToIamPolicy() =>
        new()
        {
            Version = Version,
            Statement = Statements
        };
}
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EasilyNET.Mongo.GridFS.S3.Security;

/// <summary>
///     <para xml:lang="en">MongoDB document for IAM User</para>
///     <para xml:lang="zh">IAM用户的MongoDB文档</para>
/// </summary>
[BsonIgnoreExtraElements]
public class IamUserDocument
{
    /// <summary>
    ///     <para xml:lang="en">Document ID</para>
    ///     <para xml:lang="zh">文档ID</para>
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    ///     <para xml:lang="en">User ID</para>
    ///     <para xml:lang="zh">用户ID</para>
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">User name</para>
    ///     <para xml:lang="zh">用户名</para>
    /// </summary>
    [BsonElement("userName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Access Key</para>
    ///     <para xml:lang="zh">访问密钥</para>
    /// </summary>
    [BsonElement("accessKey")]
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Secret Key</para>
    ///     <para xml:lang="zh">秘密密钥</para>
    /// </summary>
    [BsonElement("secretKey")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Associated policies</para>
    ///     <para xml:lang="zh">关联的策略</para>
    /// </summary>
    [BsonElement("policies")]
    public List<string> Policies { get; set; } = [];

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
}
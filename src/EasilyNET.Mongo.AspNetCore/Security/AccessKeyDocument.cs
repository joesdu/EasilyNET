using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EasilyNET.Mongo.AspNetCore.Security;

/// <summary>
///     <para xml:lang="en">MongoDB document for Access Key</para>
///     <para xml:lang="zh">访问密钥的MongoDB文档</para>
/// </summary>
[BsonIgnoreExtraElements]
public class AccessKeyDocument
{
    /// <summary>
    ///     <para xml:lang="en">Document ID</para>
    ///     <para xml:lang="zh">文档ID</para>
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    ///     <para xml:lang="en">Access Key ID</para>
    ///     <para xml:lang="zh">访问密钥ID</para>
    /// </summary>
    [BsonElement("accessKeyId")]
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Secret Access Key</para>
    ///     <para xml:lang="zh">秘密访问密钥</para>
    /// </summary>
    [BsonElement("secretAccessKey")]
    public string SecretAccessKey { get; set; } = string.Empty;

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
    ///     <para xml:lang="en">Status</para>
    ///     <para xml:lang="zh">状态</para>
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = "Active";

    /// <summary>
    ///     <para xml:lang="en">Creation time</para>
    ///     <para xml:lang="zh">创建时间</para>
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     <para xml:lang="en">Last used time</para>
    ///     <para xml:lang="zh">最后使用时间</para>
    /// </summary>
    [BsonElement("lastUsed")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastUsed { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Is enabled</para>
    ///     <para xml:lang="zh">是否启用</para>
    /// </summary>
    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; } = true;
}
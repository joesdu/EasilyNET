using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EasilyNET.Mongo.AspNetCore.Models;

/// <summary>
///     <para xml:lang="en">Strongly-typed GridFS file document</para>
///     <para xml:lang="zh">强类型的 GridFS 文件文档</para>
/// </summary>
public sealed class GridFSFileDocument
{
    /// <summary>
    ///     <para xml:lang="en">File document ID</para>
    ///     <para xml:lang="zh">文件文档 ID</para>
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    ///     <para xml:lang="en">File length in bytes</para>
    ///     <para xml:lang="zh">文件长度（字节）</para>
    /// </summary>
    [BsonElement("length")]
    public long Length { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Chunk size in bytes</para>
    ///     <para xml:lang="zh">块大小（字节）</para>
    /// </summary>
    [BsonElement("chunkSize")]
    public int ChunkSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Upload date</para>
    ///     <para xml:lang="zh">上传日期</para>
    /// </summary>
    [BsonElement("uploadDate")]
    public DateTime UploadDate { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </summary>
    [BsonElement("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Content type (MIME type)</para>
    ///     <para xml:lang="zh">内容类型（MIME 类型）</para>
    /// </summary>
    [BsonElement("contentType")]
    [BsonIgnoreIfNull]
    public string? ContentType { get; set; }

    /// <summary>
    ///     <para xml:lang="en">File metadata</para>
    ///     <para xml:lang="zh">文件元数据</para>
    /// </summary>
    [BsonElement("metadata")]
    public GridFSFileMetadata Metadata { get; set; } = new();
}

/// <summary>
///     <para xml:lang="en">GridFS file metadata</para>
///     <para xml:lang="zh">GridFS 文件元数据</para>
/// </summary>
public sealed class GridFSFileMetadata
{
    /// <summary>
    ///     <para xml:lang="en">File SHA256 hash</para>
    ///     <para xml:lang="zh">文件 SHA256 哈希值</para>
    /// </summary>
    [BsonElement("fileHash")]
    [BsonIgnoreIfNull]
    public string? FileHash { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Reference count for deduplication</para>
    ///     <para xml:lang="zh">去重引用计数</para>
    /// </summary>
    [BsonElement("refCount")]
    public int RefCount { get; set; } = 1;

    /// <summary>
    ///     <para xml:lang="en">Content type stored in metadata</para>
    ///     <para xml:lang="zh">存储在元数据中的内容类型</para>
    /// </summary>
    [BsonElement("contentType")]
    [BsonIgnoreIfNull]
    public string? ContentType { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Additional custom metadata</para>
    ///     <para xml:lang="zh">额外的自定义元数据</para>
    /// </summary>
    [BsonExtraElements]
    public BsonDocument? ExtraElements { get; set; }
}

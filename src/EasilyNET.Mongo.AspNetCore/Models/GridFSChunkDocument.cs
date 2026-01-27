using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EasilyNET.Mongo.AspNetCore.Models;

/// <summary>
///     <para xml:lang="en">Strongly-typed GridFS chunk document</para>
///     <para xml:lang="zh">强类型的 GridFS 块文档</para>
/// </summary>
public sealed class GridFSChunkDocument
{
    /// <summary>
    ///     <para xml:lang="en">Chunk document ID</para>
    ///     <para xml:lang="zh">块文档 ID</para>
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Reference to the parent file</para>
    ///     <para xml:lang="zh">父文件引用</para>
    /// </summary>
    [BsonElement("files_id")]
    public ObjectId FilesId { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Chunk sequence number (0-based)</para>
    ///     <para xml:lang="zh">块序号（从 0 开始）</para>
    /// </summary>
    [BsonElement("n")]
    public int N { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Chunk binary data</para>
    ///     <para xml:lang="zh">块二进制数据</para>
    /// </summary>
    [BsonElement("data")]
    public byte[] Data { get; set; } = [];
}

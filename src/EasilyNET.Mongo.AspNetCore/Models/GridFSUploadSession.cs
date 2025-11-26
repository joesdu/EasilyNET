using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo.AspNetCore.Models;

/// <summary>
///     <para xml:lang="en">GridFS resumable upload session</para>
///     <para xml:lang="zh">GridFS 断点续传会话</para>
/// </summary>
public sealed class GridFSUploadSession
{
    /// <summary>
    ///     <para xml:lang="en">Session ID</para>
    ///     <para xml:lang="zh">会话 ID</para>
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Original filename</para>
    ///     <para xml:lang="zh">原始文件名</para>
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Total file size in bytes</para>
    ///     <para xml:lang="zh">文件总大小(字节)</para>
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Current uploaded size in bytes</para>
    ///     <para xml:lang="zh">已上传大小(字节)</para>
    /// </summary>
    public long UploadedSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Chunk size in bytes</para>
    ///     <para xml:lang="zh">块大小(字节)</para>
    /// </summary>
    public int ChunkSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">GridFS file ID (null if not finalized)</para>
    ///     <para xml:lang="zh">GridFS 文件 ID(未完成时为 null)</para>
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public string? FileId { get; set; }

    /// <summary>
    ///     <para xml:lang="en">File metadata</para>
    ///     <para xml:lang="zh">文件元数据</para>
    /// </summary>
    public BsonDocument? Metadata { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Uploaded chunk numbers</para>
    ///     <para xml:lang="zh">已上传的块编号列表</para>
    /// </summary>
    public List<int> UploadedChunks { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">Session creation time</para>
    ///     <para xml:lang="zh">会话创建时间</para>
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     <para xml:lang="en">Last update time</para>
    ///     <para xml:lang="zh">最后更新时间</para>
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     <para xml:lang="en">Session expiration time</para>
    ///     <para xml:lang="zh">会话过期时间</para>
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Upload status</para>
    ///     <para xml:lang="zh">上传状态</para>
    /// </summary>
    public UploadStatus Status { get; set; } = UploadStatus.InProgress;

    /// <summary>
    ///     <para xml:lang="en">File hash (MD5/SHA256)</para>
    ///     <para xml:lang="zh">文件哈希值(MD5/SHA256)</para>
    /// </summary>
    public string? FileHash { get; set; }
}

/// <summary>
///     <para xml:lang="en">Upload status enumeration</para>
///     <para xml:lang="zh">上传状态枚举</para>
/// </summary>
public enum UploadStatus
{
    /// <summary>
    ///     <para xml:lang="en">Upload in progress</para>
    ///     <para xml:lang="zh">上传中</para>
    /// </summary>
    InProgress = 0,

    /// <summary>
    ///     <para xml:lang="en">Upload completed</para>
    ///     <para xml:lang="zh">上传完成</para>
    /// </summary>
    Completed = 1,

    /// <summary>
    ///     <para xml:lang="en">Upload failed</para>
    ///     <para xml:lang="zh">上传失败</para>
    /// </summary>
    Failed = 2,

    /// <summary>
    ///     <para xml:lang="en">Upload cancelled</para>
    ///     <para xml:lang="zh">上传取消</para>
    /// </summary>
    Cancelled = 3,

    /// <summary>
    ///     <para xml:lang="en">Session expired</para>
    ///     <para xml:lang="zh">会话过期</para>
    /// </summary>
    Expired = 4
}
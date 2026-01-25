using EasilyNET.Mongo.AspNetCore.Models;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.Mongo.AspNetCore.Abstraction;

/// <summary>
///     <para xml:lang="en">GridFS resumable upload service interface for testability and abstraction</para>
///     <para xml:lang="zh">GridFS 断点续传服务接口，用于可测试性和抽象化</para>
/// </summary>
public interface IGridFSUploadService
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a new resumable upload session. Returns session ID that can be used to resume upload later.
    ///     Supports instant upload (deduplication) if file hash matches existing file.
    ///     </para>
    ///     <para xml:lang="zh">创建新的断点续传会话。返回会话 ID，可用于稍后恢复上传。如果文件哈希匹配现有文件，支持秒传（去重）。</para>
    /// </summary>
    /// <param name="filename">
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </param>
    /// <param name="totalSize">
    ///     <para xml:lang="en">Total file size in bytes</para>
    ///     <para xml:lang="zh">文件总大小(字节)</para>
    /// </param>
    /// <param name="fileHash">
    ///     <para xml:lang="en">File SHA256 hash for deduplication (optional)</para>
    ///     <para xml:lang="zh">文件 SHA256 哈希值，用于去重(可选)</para>
    /// </param>
    /// <param name="contentType">
    ///     <para xml:lang="en">File content type (optional)</para>
    ///     <para xml:lang="zh">文件类型(可选)</para>
    /// </param>
    /// <param name="chunkSize">
    ///     <para xml:lang="en">Chunk size in bytes (optional, uses optimal size if not specified)</para>
    ///     <para xml:lang="zh">块大小(可选，未指定时使用最优大小)</para>
    /// </param>
    /// <param name="sessionExpirationHours">
    ///     <para xml:lang="en">Session expiration time in hours (default: 24 hours)</para>
    ///     <para xml:lang="zh">会话过期时间(小时，默认 24 小时)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Upload session</para>
    ///     <para xml:lang="zh">上传会话</para>
    /// </returns>
    Task<GridFSUploadSession> CreateSessionAsync(
        string filename,
        long totalSize,
        string? fileHash,
        string? contentType = null,
        int? chunkSize = null,
        int sessionExpirationHours = 24,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Get upload session by session ID</para>
    ///     <para xml:lang="zh">通过会话 ID 获取上传会话</para>
    /// </summary>
    /// <param name="sessionId">
    ///     <para xml:lang="en">Session ID</para>
    ///     <para xml:lang="zh">会话 ID</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Upload session or null if not found</para>
    ///     <para xml:lang="zh">上传会话，如果未找到则返回 null</para>
    /// </returns>
    Task<GridFSUploadSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Upload a chunk of data. Supports uploading chunks in any order (out-of-order upload).
    ///     </para>
    ///     <para xml:lang="zh">上传数据块。支持乱序上传(任意顺序上传块)。</para>
    /// </summary>
    /// <param name="sessionId">
    ///     <para xml:lang="en">Session ID</para>
    ///     <para xml:lang="zh">会话 ID</para>
    /// </param>
    /// <param name="chunkNumber">
    ///     <para xml:lang="en">Chunk number (0-based)</para>
    ///     <para xml:lang="zh">块编号(从 0 开始)</para>
    /// </param>
    /// <param name="data">
    ///     <para xml:lang="en">Chunk data</para>
    ///     <para xml:lang="zh">块数据</para>
    /// </param>
    /// <param name="chunkHash">
    ///     <para xml:lang="en">Chunk SHA256 hash for verification</para>
    ///     <para xml:lang="zh">块 SHA256 哈希值，用于校验</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Updated session</para>
    ///     <para xml:lang="zh">更新后的会话</para>
    /// </returns>
    Task<GridFSUploadSession> UploadChunkAsync(
        string sessionId,
        int chunkNumber,
        byte[] data,
        string chunkHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Finalize upload - combines all chunks into a complete GridFS file. Call this after all chunks are uploaded.
    ///     </para>
    ///     <para xml:lang="zh">完成上传 - 将所有块组合成完整的 GridFS 文件。在所有块上传完成后调用。</para>
    /// </summary>
    /// <param name="sessionId">
    ///     <para xml:lang="en">Session ID</para>
    ///     <para xml:lang="zh">会话 ID</para>
    /// </param>
    /// <param name="verifyHash">
    ///     <para xml:lang="en">Expected file hash (SHA256) for verification (optional)</para>
    ///     <para xml:lang="zh">用于验证的预期文件哈希值(SHA256)(可选)</para>
    /// </param>
    /// <param name="skipHashValidation">
    ///     <para xml:lang="en">Skip full hash validation (trust client hash, faster but less safe)</para>
    ///     <para xml:lang="zh">跳过服务器端全量哈希校验(依赖客户端哈希，更快但安全性降低)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">GridFS file ID</para>
    ///     <para xml:lang="zh">GridFS 文件 ID</para>
    /// </returns>
    Task<ObjectId> FinalizeUploadAsync(
        string sessionId,
        string? verifyHash = null,
        bool skipHashValidation = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Get missing chunk numbers for a session</para>
    ///     <para xml:lang="zh">获取会话中缺失的块编号</para>
    /// </summary>
    /// <param name="sessionId">
    ///     <para xml:lang="en">Session ID</para>
    ///     <para xml:lang="zh">会话 ID</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">List of missing chunk numbers</para>
    ///     <para xml:lang="zh">缺失的块编号列表</para>
    /// </returns>
    Task<List<int>> GetMissingChunksAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Cancel upload session and clean up temporary data</para>
    ///     <para xml:lang="zh">取消上传会话并清理临时数据</para>
    /// </summary>
    /// <param name="sessionId">
    ///     <para xml:lang="en">Session ID</para>
    ///     <para xml:lang="zh">会话 ID</para>
    /// </param>
    /// <param name="deleteSession">
    ///     <para xml:lang="en">Whether to delete the session record (default: true). If false, only marks as cancelled.</para>
    ///     <para xml:lang="zh">是否删除会话记录(默认: true)。如果为 false，仅标记为已取消。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task CancelSessionAsync(string sessionId, bool deleteSession = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Downloads a range of bytes from a GridFS file. Supports HTTP Range header for video/audio streaming.
    ///     </para>
    ///     <para xml:lang="zh">从 GridFS 文件中下载指定范围的字节。支持 HTTP Range 头，用于视频/音频流传输。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">File ObjectId</para>
    ///     <para xml:lang="zh">文件 ObjectId</para>
    /// </param>
    /// <param name="startByte">
    ///     <para xml:lang="en">Start byte position (inclusive)</para>
    ///     <para xml:lang="zh">起始字节位置(包含)</para>
    /// </param>
    /// <param name="endByte">
    ///     <para xml:lang="en">End byte position (inclusive), null for end of file</para>
    ///     <para xml:lang="zh">结束字节位置(包含)，null 表示文件末尾</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Range stream with file info</para>
    ///     <para xml:lang="zh">范围流及文件信息</para>
    /// </returns>
    Task<(Stream Stream, long TotalLength, long RangeStart, long RangeEnd, GridFSFileInfo FileInfo)> DownloadRangeAsync(
        ObjectId id,
        long startByte,
        long? endByte = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Rename a file in GridFS</para>
    ///     <para xml:lang="zh">重命名 GridFS 中的文件</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">File ID</para>
    ///     <para xml:lang="zh">文件 ID</para>
    /// </param>
    /// <param name="newName">
    ///     <para xml:lang="en">New filename</para>
    ///     <para xml:lang="zh">新文件名</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task RenameAsync(string id, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Delete files by IDs with reference counting</para>
    ///     <para xml:lang="zh">根据 ID 删除文件（带引用计数）</para>
    /// </summary>
    /// <param name="ids">
    ///     <para xml:lang="en">File IDs to delete</para>
    ///     <para xml:lang="zh">要删除的文件 ID 集合</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Deleted filenames</para>
    ///     <para xml:lang="zh">已删除的文件名</para>
    /// </returns>
    Task<IEnumerable<string>> DeleteAsync(string[] ids, CancellationToken cancellationToken = default);
}
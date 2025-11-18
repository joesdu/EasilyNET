using EasilyNET.Mongo.AspNetCore.Helpers;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedMember.Global

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// GridFS 断点续传控制器
/// </summary>
[ApiController]
[Route("api/GridFS/Resumable")]
[ApiExplorerSettings(GroupName = "MongoFS")]
public class GridFSResumableController(GridFSBucket bucket) : ControllerBase
{
    private GridFSResumableUploadHelper ResumableHelper => field ??= new(bucket);

    /// <summary>
    /// 创建断点续传会话
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <param name="totalSize">文件总大小(字节)</param>
    /// <param name="contentType">Content-Type</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("CreateSession")]
    public virtual async Task<IActionResult> CreateSession(
        [FromQuery]
        string filename,
        [FromQuery]
        long totalSize,
        [FromQuery]
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        var metadata = new BsonDocument();
        if (!string.IsNullOrEmpty(contentType))
        {
            metadata["contentType"] = contentType;
        }
        var session = await ResumableHelper.CreateSessionAsync(filename,
                          totalSize,
                          metadata,
                          cancellationToken: cancellationToken);
        return Ok(new
        {
            sessionId = session.SessionId,
            filename = session.Filename,
            totalSize = session.TotalSize,
            chunkSize = session.ChunkSize,
            expiresAt = session.ExpiresAt
        });
    }

    /// <summary>
    /// 上传文件块
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="chunkNumber">块编号(从 0 开始)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("UploadChunk")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB max chunk size
    public virtual async Task<IActionResult> UploadChunk(
        [FromQuery]
        string sessionId,
        [FromQuery]
        int chunkNumber,
        CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, cancellationToken);
        var data = ms.ToArray();
        try
        {
            var session = await ResumableHelper.UploadChunkAsync(sessionId, chunkNumber, data, cancellationToken);
            return Ok(new
            {
                sessionId = session.SessionId,
                chunkNumber,
                uploadedSize = session.UploadedSize,
                totalSize = session.TotalSize,
                progress = ((double)session.UploadedSize / session.TotalSize) * 100,
                uploadedChunks = session.UploadedChunks.Count
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取上传会话信息
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("Session/{sessionId}")]
    public virtual async Task<IActionResult> GetSession(string sessionId, CancellationToken cancellationToken)
    {
        var session = await ResumableHelper.GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return NotFound($"Session {sessionId} not found");
        }
        var missingChunks = await ResumableHelper.GetMissingChunksAsync(sessionId, cancellationToken);
        return Ok(new
        {
            sessionId = session.SessionId,
            filename = session.Filename,
            totalSize = session.TotalSize,
            uploadedSize = session.UploadedSize,
            chunkSize = session.ChunkSize,
            progress = ((double)session.UploadedSize / session.TotalSize) * 100,
            uploadedChunks = session.UploadedChunks,
            missingChunks,
            status = session.Status.ToString(),
            createdAt = session.CreatedAt,
            updatedAt = session.UpdatedAt,
            expiresAt = session.ExpiresAt
        });
    }

    /// <summary>
    /// 获取缺失的块编号列表
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("MissingChunks/{sessionId}")]
    public virtual async Task<IActionResult> GetMissingChunks(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var missingChunks = await ResumableHelper.GetMissingChunksAsync(sessionId, cancellationToken);
            return Ok(new { sessionId, missingChunks });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// 完成上传 - 将所有块组合成完整文件
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="fileHash">文件哈希值(可选,用于验证)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("Finalize/{sessionId}")]
    public virtual async Task<IActionResult> FinalizeUpload(
        string sessionId,
        [FromQuery]
        string? fileHash = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileId = await ResumableHelper.FinalizeUploadAsync(sessionId, fileHash, cancellationToken);
            return Ok(new
            {
                fileId = fileId.ToString(),
                message = "Upload completed successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 取消上传会话
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("Cancel/{sessionId}")]
    public virtual async Task<IActionResult> CancelUpload(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            await ResumableHelper.CancelSessionAsync(sessionId, cancellationToken);
            return Ok(new { message = "Upload cancelled successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
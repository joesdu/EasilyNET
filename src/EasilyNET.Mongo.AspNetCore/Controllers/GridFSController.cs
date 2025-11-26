using EasilyNET.Mongo.AspNetCore.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedMember.Global

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// GridFS 断点续传控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "MongoFS")]
public class GridFSController(IGridFSBucket bucket, ILogger<GridFSController> logger) : ControllerBase
{
    /// <summary>
    /// GridFSFileInfo Filter
    /// </summary>
    private readonly FilterDefinitionBuilder<GridFSFileInfo> gbf = Builders<GridFSFileInfo>.Filter;

    private GridFSResumableUploadHelper ResumableHelper => field ??= new(bucket);

    /// <summary>
    /// 创建断点续传会话
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <param name="totalSize">文件总大小(字节)</param>
    /// <param name="fileHash">文件SHA256特征值</param>
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
        string? fileHash = null,
        [FromQuery]
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        var metadata = new BsonDocument();
        if (!string.IsNullOrEmpty(contentType))
        {
            metadata["contentType"] = contentType;
        }
        var session = await ResumableHelper.CreateSessionAsync(filename, totalSize, fileHash, metadata, cancellationToken: cancellationToken);
        return Ok(new
        {
            sessionId = session.SessionId,
            filename = session.Filename,
            totalSize = session.TotalSize,
            chunkSize = session.ChunkSize,
            expiresAt = session.ExpiresAt,
            status = session.Status.ToString(),
            fileId = session.FileId // 如果秒传成功,这里会有值
        });
    }

    /// <summary>
    /// 上传文件块
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="chunkNumber">块编号(从 0 开始)</param>
    /// <param name="chunkHash">块 SHA256 哈希</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("UploadChunk")]
    public virtual async Task<IActionResult> UploadChunk(
        [FromQuery]
        string sessionId,
        [FromQuery]
        int chunkNumber,
        [FromQuery]
        string chunkHash,
        CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, cancellationToken);
        var data = ms.ToArray();
        try
        {
            var session = await ResumableHelper.UploadChunkAsync(sessionId, chunkNumber, data, chunkHash, cancellationToken);
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
    public virtual async Task<IActionResult> GetSession(string sessionId, CancellationToken cancellationToken = default)
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
    public virtual async Task<IActionResult> GetMissingChunks(string sessionId, CancellationToken cancellationToken = default)
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
    /// <param name="fileHash">文件哈希值(SHA256,可选,用于验证)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("Finalize/{sessionId}")]
    public virtual async Task<IActionResult> FinalizeUpload(string sessionId, [FromQuery] string? fileHash = null, CancellationToken cancellationToken = default)
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
            logger.LogError("FinalizeUpload InvalidOperationException: {ExMessage}", ex.Message);
            logger.LogError("StackTrace: {ExStackTrace}", ex.StackTrace);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError("FinalizeUpload Exception: {Name}", ex.GetType().Name);
            logger.LogError("Message: {ExMessage}", ex.Message);
            logger.LogError("StackTrace: {ExStackTrace}", ex.StackTrace);
            // ReSharper disable once InvertIf
            if (ex.InnerException is not null)
            {
                logger.LogError("InnerException: {InnerExceptionMessage}", ex.InnerException.Message);
                logger.LogError("InnerException StackTrace: {InnerExceptionStackTrace}", ex.InnerException.StackTrace);
            }
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// 取消上传会话
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("Cancel/{sessionId}")]
    public virtual async Task<IActionResult> CancelUpload(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 默认删除会话记录
            await ResumableHelper.CancelSessionAsync(sessionId, true, cancellationToken);
            return Ok(new { message = "Upload cancelled successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 流式下载 - 支持 Range 请求,用于视频/音频播放
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("StreamRange/{id}")]
    public virtual async Task<IActionResult> StreamRange(string id, CancellationToken cancellationToken = default)
    {
        // 尝试解析为 ObjectId (直接 FileId)
        if (!ObjectId.TryParse(id, out var fileId))
        {
            // 不是 ObjectId, 尝试作为 SessionId 查询
            var session = await ResumableHelper.GetSessionAsync(id, cancellationToken);
            if (session != null && session.Status.ToString() == "Completed" && !string.IsNullOrEmpty(session.FileId))
            {
                fileId = ObjectId.Parse(session.FileId);
            }
            else
            {
                return NotFound($"File or Session {id} not found or not ready.");
            }
        }

        // 解析 Range 头
        var rangeHeader = Request.Headers[HeaderNames.Range].ToString();
        long? startByte = null;
        long? endByte = null;
        if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
        {
            var range = rangeHeader[6..].Split('-');
            if (range.Length == 2)
            {
                if (long.TryParse(range[0], out var start))
                {
                    startByte = start;
                }
                if (!string.IsNullOrEmpty(range[1]) && long.TryParse(range[1], out var end))
                {
                    endByte = end;
                }
            }
        }
        try
        {
            var result = await GridFSRangeStreamHelper.DownloadRangeAsync(bucket, fileId, startByte ?? 0, endByte, cancellationToken);
            var contentType = result.FileInfo.Metadata.Contains("contentType")
                                  ? result.FileInfo.Metadata["contentType"].AsString
                                  : "application/octet-stream";
            // 设置响应头
            Response.Headers[HeaderNames.AcceptRanges] = "bytes";
            Response.Headers[HeaderNames.ContentRange] = $"bytes {result.RangeStart}-{result.RangeEnd}/{result.TotalLength}";
            Response.StatusCode = startByte.HasValue ? 206 : 200; // 206 Partial Content
            return File(result.Stream, contentType, result.FileInfo.Filename, false);
        }
        catch (ArgumentOutOfRangeException)
        {
            return StatusCode(416); // Range Not Satisfiable
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 客户端取消请求(正常行为,如视频快进/快退)
            logger.LogDebug("Range request cancelled by client for file {FileId}", id);
            return StatusCode(499); // Client Closed Request
        }
    }

    /// <summary>
    /// 重命名文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="newName">新名称</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id}/Rename/{newName}")]
    // ReSharper disable once MemberCanBeProtected.Global
    public virtual async Task Rename(string id, string newName, CancellationToken cancellationToken = default)
    {
        await bucket.RenameAsync(ObjectId.Parse(id), newName, cancellationToken);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="ids">文件ID集合</param>
    /// <returns></returns>
    [HttpDelete]
    public virtual async Task<IEnumerable<string>> Delete(string[] ids, CancellationToken cancellationToken = default)
    {
        var oids = ids.Select(ObjectId.Parse).ToList();
        var fi = await (await bucket.FindAsync(gbf.In(c => c.Id, oids), cancellationToken: cancellationToken)).ToListAsync(cancellationToken);
        var fids = fi.Select(c => new { Id = c.Id.ToString(), FileName = c.Filename }).ToArray();
        // 删除 GridFS 中的文件 (使用引用计数删除)
        foreach (var item in fids)
        {
            await ResumableHelper.DeleteFileAsync(ObjectId.Parse(item.Id), cancellationToken);
        }
        return fids.Select(c => c.FileName);
    }
}
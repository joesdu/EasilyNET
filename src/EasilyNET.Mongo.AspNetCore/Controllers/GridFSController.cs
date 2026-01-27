using EasilyNET.Mongo.AspNetCore.Helpers;
using EasilyNET.Mongo.AspNetCore.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.Controllers;

/// <summary>
/// GridFS 断点续传控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "MongoFS")]
public sealed class GridFSController(
    GridFSHelper resumableHelper,
    ILogger<GridFSController> logger,
    GridFSRateLimiter? rateLimiter = null,
    IOptions<GridFSRateLimitOptions>? uploadOptions = null) : ControllerBase
{
    private readonly GridFSRateLimitOptions _uploadOptions = uploadOptions?.Value ?? new();

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
    public async Task<IActionResult> CreateSession(
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
        // 验证文件大小限制
        if (_uploadOptions.MaxFileSize > 0 && totalSize > _uploadOptions.MaxFileSize)
        {
            return BadRequest($"File size {totalSize} exceeds maximum allowed size {_uploadOptions.MaxFileSize}");
        }

        // 尝试获取会话槽位（速率限制）
        if (rateLimiter is not null && !await rateLimiter.TryAcquireSessionSlotAsync(cancellationToken))
        {
            return StatusCode(429, "Too many concurrent upload sessions. Please try again later.");
        }

        try
        {
            var session = await resumableHelper.CreateSessionAsync(filename, totalSize, fileHash, contentType, cancellationToken: cancellationToken);
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
        catch (Exception)
        {
            // 如果创建会话失败，释放槽位
            rateLimiter?.ReleaseSessionSlot();
            throw;
        }
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
    public async Task<IActionResult> UploadChunk(
        [FromQuery]
        string sessionId,
        [FromQuery]
        int chunkNumber,
        [FromQuery]
        string chunkHash,
        CancellationToken cancellationToken = default)
    {
        // 尝试获取块上传槽位（速率限制）
        if (rateLimiter is not null && !await rateLimiter.TryAcquireChunkSlotAsync(sessionId, cancellationToken))
        {
            return StatusCode(429, "Too many concurrent chunk uploads. Please try again later.");
        }

        try
        {
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms, cancellationToken);
            var data = ms.ToArray();

            // 验证块大小限制
            if (data.Length > _uploadOptions.MaxChunkSize)
            {
                return BadRequest($"Chunk size {data.Length} exceeds maximum allowed size {_uploadOptions.MaxChunkSize}");
            }

            var session = await resumableHelper.UploadChunkAsync(sessionId, chunkNumber, data, chunkHash, cancellationToken);
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
        finally
        {
            rateLimiter?.ReleaseChunkSlot(sessionId);
        }
    }

    /// <summary>
    /// 获取上传会话信息
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("Session/{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await resumableHelper.GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return NotFound($"Session {sessionId} not found");
        }
        var missingChunks = await resumableHelper.GetMissingChunksAsync(sessionId, cancellationToken);
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
    public async Task<IActionResult> GetMissingChunks(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var missingChunks = await resumableHelper.GetMissingChunksAsync(sessionId, cancellationToken);
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
    /// <param name="skipHashValidation">是否跳过服务器端全量哈希校验(更快但安全性降低)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("Finalize/{sessionId}")]
    public async Task<IActionResult> FinalizeUpload(string sessionId, [FromQuery] string? fileHash = null, [FromQuery] bool skipHashValidation = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileId = await resumableHelper.FinalizeUploadAsync(sessionId, fileHash, skipHashValidation, cancellationToken);

            // 上传完成，释放速率限制器资源
            rateLimiter?.RemoveSession(sessionId);
            rateLimiter?.ReleaseSessionSlot();

            return Ok(new
            {
                fileId = fileId.ToString(),
                message = "Upload completed successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            // ReSharper disable once InvertIf
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("FinalizeUpload InvalidOperationException: {ExMessage}", ex.Message);
                logger.LogError("StackTrace: {ExStackTrace}", ex.StackTrace);
            }
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("FinalizeUpload Exception: {Name}", ex.GetType().Name);
                logger.LogError("Message: {ExMessage}", ex.Message);
                logger.LogError("StackTrace: {ExStackTrace}", ex.StackTrace);
            }
            // ReSharper disable once InvertIf
            if (ex.InnerException is not null && logger.IsEnabled(LogLevel.Error))
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
    public async Task<IActionResult> CancelUpload(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 默认删除会话记录
            await resumableHelper.CancelSessionAsync(sessionId, true, cancellationToken);

            // 释放速率限制器资源
            rateLimiter?.RemoveSession(sessionId);
            rateLimiter?.ReleaseSessionSlot();

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
    public async Task<IActionResult> StreamRange(string id, CancellationToken cancellationToken = default)
    {
        // 尝试解析为 ObjectId (直接 FileId)
        if (!ObjectId.TryParse(id, out var fileId))
        {
            // 不是 ObjectId, 尝试作为 SessionId 查询
            var session = await resumableHelper.GetSessionAsync(id, cancellationToken);
            if (session != null && session.Status.ToString() == "Completed" && !string.IsNullOrEmpty(session.FileId))
            {
                fileId = ObjectId.Parse(session.FileId);
            }
            else
            {
                return NotFound($"File or Session {id} not found or not ready.");
            }
        }
        try
        {
            // 获取完整的可定位流,让 ASP.NET Core 内置的 Range 处理机制自动处理
            var result = await resumableHelper.DownloadFullStreamAsync(fileId, cancellationToken);
            
            // 调试日志：检查流的属性
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("StreamRange: FileId={FileId}, FileName={FileName}, FileLength={FileLength}, StreamCanSeek={CanSeek}, StreamLength={StreamLength}, StreamPosition={Position}",
                    fileId, result.FileInfo.Filename, result.FileInfo.Length, 
                    result.Stream.CanSeek, 
                    result.Stream.CanSeek ? result.Stream.Length : -1,
                    result.Stream.CanSeek ? result.Stream.Position : -1);
            }
            
            // 优先从 metadata.contentType 读取，如果没有则尝试从顶层 contentType 字段读取
            // 最后根据文件扩展名推断
            string contentType;
            if (result.FileInfo.Metadata.Contains("contentType"))
            {
                contentType = result.FileInfo.Metadata["contentType"].AsString;
            }
            else
            {
                // 根据文件扩展名推断 MIME 类型
                var ext = Path.GetExtension(result.FileInfo.Filename)?.ToLowerInvariant();
                contentType = ext switch
                {
                    ".mp4"  => "video/mp4",
                    ".webm" => "video/webm",
                    ".ogg"  => "video/ogg",
                    ".ogv"  => "video/ogg",
                    ".mov"  => "video/quicktime",
                    ".avi"  => "video/x-msvideo",
                    ".mkv"  => "video/x-matroska",
                    ".mp3"  => "audio/mpeg",
                    ".wav"  => "audio/wav",
                    ".flac" => "audio/flac",
                    ".aac"  => "audio/aac",
                    ".m4a"  => "audio/mp4",
                    ".jpg"  => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png"  => "image/png",
                    ".gif"  => "image/gif",
                    ".webp" => "image/webp",
                    ".svg"  => "image/svg+xml",
                    ".pdf"  => "application/pdf",
                    ".json" => "application/json",
                    ".xml"  => "application/xml",
                    ".zip"  => "application/zip",
                    ".txt"  => "text/plain",
                    ".html" => "text/html",
                    ".css"  => "text/css",
                    ".js"   => "application/javascript",
                    _       => "application/octet-stream"
                };
            }
            
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("StreamRange: ContentType={ContentType}, RangeHeader={RangeHeader}",
                    contentType, Request.Headers.Range.ToString());
            }
            
            // 获取文件的上传时间作为 LastModified
            var lastModified = new DateTimeOffset(result.FileInfo.UploadDateTime, TimeSpan.Zero);
            // 使用文件ID和上传时间生成 ETag
            var etag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{result.FileInfo.Id}_{result.FileInfo.UploadDateTime.Ticks}\"");
            // 使用 enableRangeProcessing: true 让 ASP.NET Core 自动处理 Range 请求
            // 框架会自动:
            // 1. 解析 Range 头
            // 2. 设置正确的 Content-Range 和 Content-Length
            // 3. 返回 206 Partial Content 或 200 OK
            // 4. 只发送请求的字节范围
            return File(result.Stream, contentType, result.FileInfo.Filename, lastModified, etag, enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return NotFound($"File {id} not found.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 客户端取消请求(正常行为,如视频快进/快退)
            // Sanitize 'id' to prevent log forging by removing newlines/carriage returns.
            var sanitizedId = id.Replace("\r", "").Replace("\n", "");
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Range request cancelled by client for file {FileId}", sanitizedId);
            }
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
    public async Task Rename(string id, string newName, CancellationToken cancellationToken = default) => await resumableHelper.Rename(id, newName, cancellationToken);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="ids">文件ID集合</param>
    /// <returns></returns>
    [HttpDelete]
    public async Task<IEnumerable<string>> Delete(string[] ids, CancellationToken cancellationToken = default) => await resumableHelper.Delete(ids, cancellationToken);
}
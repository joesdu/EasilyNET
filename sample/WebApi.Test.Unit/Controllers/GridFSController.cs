using System.Collections.Concurrent;
using EasilyNET.Core;
using EasilyNET.Mongo.AspNetCore.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using WebApi.Test.Unit.dtos;

// ReSharper disable UnusedMemberHierarchy.Global
// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable UnusedMember.Global

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// GridFS控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "MongoFS")]
public class GridFSController(IGridFSBucket bucket) : ControllerBase
{
    /// <summary>
    /// 查询过滤器
    /// </summary>
    private readonly FilterDefinitionBuilder<GridFSItemInfo> _bf = Builders<GridFSItemInfo>.Filter;

    /// <summary>
    /// IMongoCollection
    /// </summary>
    private readonly IMongoCollection<GridFSItemInfo> Coll = bucket.Database.GetCollection<GridFSItemInfo>("item.infos");

    /// <summary>
    /// GridFSFileInfo Filter
    /// </summary>
    private readonly FilterDefinitionBuilder<GridFSFileInfo> gbf = Builders<GridFSFileInfo>.Filter;

    /// <summary>
    /// 获取已上传文件列表
    /// </summary>
    /// <param name="info">关键字支持:文件名,用户名,用户ID,App名称以及业务名称模糊匹配</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("Infos")]
    public virtual async Task<object> Infos(InfoSearch info, CancellationToken cancellationToken)
    {
        var filters = new List<FilterDefinition<GridFSItemInfo>>();
        if (!string.IsNullOrWhiteSpace(info.FileName))
        {
            filters.Add(_bf.Where(c => c.FileName.Contains(info.FileName)));
        }
        if (!string.IsNullOrWhiteSpace(info.UserName))
        {
            filters.Add(_bf.Where(c => c.UserName.Contains(info.UserName)));
        }
        if (!string.IsNullOrWhiteSpace(info.UserId))
        {
            filters.Add(_bf.Where(c => c.UserId.Contains(info.UserId)));
        }
        if (!string.IsNullOrWhiteSpace(info.App))
        {
            filters.Add(_bf.Where(c => c.App.Contains(info.App)));
        }
        if (!string.IsNullOrWhiteSpace(info.BusinessType))
        {
            filters.Add(_bf.Where(c => c.BusinessType.Contains(info.BusinessType)));
        }
        if (info.Start is not null)
        {
            filters.Add(_bf.Gte(c => c.CreateTime, info.Start));
        }
        if (info.End is not null)
        {
            filters.Add(_bf.Lte(c => c.CreateTime, info.End));
        }
        if (!string.IsNullOrWhiteSpace(info.Key))
        {
            filters.Add(_bf.Or(_bf.Where(c => c.FileName.Contains(info.Key!)),
                _bf.Where(c => c.UserName.Contains(info.Key!)),
                _bf.Where(c => c.UserId.Contains(info.Key!)),
                _bf.Where(c => c.App.Contains(info.Key!)),
                _bf.Where(c => c.BusinessType.Contains(info.Key!))));
        }
        var filter = filters.Count > 0 ? _bf.And(filters) : _bf.Empty;
        var total = await Coll.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var list = await Coll.Find(filter)
                             .Sort(Builders<GridFSItemInfo>.Sort.Descending(c => c.CreateTime))
                             .Skip(info.Skip)
                             .Limit(info.Size)
                             .ToListAsync(cancellationToken);
        return PageResult.Wrap(total, list);
    }

    /// <summary>
    /// 添加一个或多个文件
    /// </summary>
    [HttpPost("UploadMulti")]
    public virtual async Task<IEnumerable<GridFSItem>> PostMulti([FromForm] UploadGridFSMulti fs, CancellationToken cancellationToken)
    {
        if (fs.File is null || fs.File.Count == 0)
        {
            throw new("no files find");
        }
        if (fs.DeleteIds.Count > 0)
        {
            await Delete(cancellationToken, [.. fs.DeleteIds]);
        }
        var rsList = new ConcurrentBag<GridFSItem>();
        var infos = new ConcurrentBag<GridFSItemInfo>();
        await Parallel.ForEachAsync(fs.File, cancellationToken, async (item, token) =>
        {
            if (item.ContentType is null)
            {
                throw new("ContentType in File is null");
            }
            var metadata = new Dictionary<string, object>
            {
                { "contentType", item.ContentType }
            };
            if (!string.IsNullOrWhiteSpace(fs.BusinessType))
            {
                metadata.Add("business", fs.BusinessType);
            }
            if (!string.IsNullOrWhiteSpace(fs.CategoryId))
            {
                metadata.Add("category", fs.CategoryId!);
            }
            var upo = new GridFSUploadOptions { BatchSize = fs.File.Count, Metadata = new(metadata) };
            var oid = await bucket.UploadFromStreamAsync(item.FileName, item.OpenReadStream(), upo, token);
            rsList.Add(new() { FileId = oid.ToString() ?? string.Empty, FileName = item.FileName, Length = item.Length, ContentType = item.ContentType });
            infos.Add(new()
            {
                FileId = oid.ToString() ?? string.Empty,
                FileName = item.FileName,
                Length = item.Length,
                ContentType = item.ContentType,
                UserId = fs.UserId,
                UserName = fs.UserName,
                App = fs.App,
                BusinessType = fs.BusinessType,
                CategoryId = fs.CategoryId,
                CreateTime = DateTime.Now
            });
        });
        await Coll.InsertManyAsync(infos, cancellationToken: cancellationToken);
        return rsList;
    }

    /// <summary>
    /// 添加一个文件
    /// </summary>
    [HttpPost("UploadSingle")]
    public virtual async Task<GridFSItem> PostSingle([FromForm] UploadGridFSSingle fs, CancellationToken cancellationToken)
    {
        if (fs.File is null)
        {
            throw new("no files find");
        }
        if (!string.IsNullOrWhiteSpace(fs.DeleteId))
        {
            await Delete(cancellationToken, fs.DeleteId!);
        }
        if (fs.File.ContentType is null)
        {
            throw new("ContentType in File is null");
        }
        var metadata = new Dictionary<string, object>
        {
            { "contentType", fs.File.ContentType }
        };
        if (!string.IsNullOrWhiteSpace(fs.BusinessType))
        {
            metadata.Add("business", fs.BusinessType);
        }
        if (!string.IsNullOrWhiteSpace(fs.CategoryId))
        {
            metadata.Add("category", fs.CategoryId!);
        }
        var upo = new GridFSUploadOptions { BatchSize = 1, Metadata = new(metadata) };
        var oid = await bucket.UploadFromStreamAsync(fs.File.FileName, fs.File.OpenReadStream(), upo, cancellationToken);
        try
        {
            await Coll.InsertOneAsync(new()
            {
                FileId = oid.ToString() ?? string.Empty,
                FileName = fs.File.FileName,
                Length = fs.File.Length,
                ContentType = fs.File.ContentType,
                UserId = fs.UserId,
                UserName = fs.UserName,
                App = fs.App,
                BusinessType = fs.BusinessType,
                CategoryId = fs.CategoryId,
                CreateTime = DateTime.Now
            }, cancellationToken: cancellationToken);
        }
        catch
        {
            // 如果元数据插入失败,尝试删除已上传的文件以避免孤儿文件
            await bucket.DeleteAsync(oid, cancellationToken);
            throw;
        }
        return new() { FileId = oid.ToString() ?? string.Empty, FileName = fs.File.FileName, Length = fs.File.Length, ContentType = fs.File.ContentType };
    }

    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("Download/{id}")]
    public virtual async Task<FileStreamResult> Download(string id, CancellationToken cancellationToken)
    {
        var stream = await bucket.OpenDownloadStreamAsync(ObjectId.Parse(id), new() { Seekable = true }, cancellationToken);
        var content_type = stream.FileInfo.Metadata["contentType"].AsString;
        if (string.IsNullOrWhiteSpace(content_type))
        {
            content_type = "application/octet-stream";
        }
        return File(stream, content_type, stream.FileInfo.Filename);
    }

    /// <summary>
    /// 流式下载 - 支持 Range 请求,用于视频/音频播放
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("StreamRange/{id}")]
    public virtual async Task<IActionResult> StreamRange(string id, CancellationToken cancellationToken)
    {
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
            var result = await GridFSRangeStreamHelper.DownloadRangeAsync(bucket,
                             ObjectId.Parse(id),
                             startByte ?? 0,
                             endByte,
                             cancellationToken);
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
    }

    /// <summary>
    /// 通过文件名流式下载 - 支持 Range 请求
    /// </summary>
    /// <param name="name">文件名</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("StreamRangeByName/{name}")]
    public virtual async Task<IActionResult> StreamRangeByName(string name, CancellationToken cancellationToken)
    {
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
            var result = await GridFSRangeStreamHelper.DownloadRangeByNameAsync(bucket,
                             name,
                             startByte ?? 0,
                             endByte,
                             cancellationToken);
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
    }

    /// <summary>
    /// 打开文件内容
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("FileContent/{id}")]
    public virtual async Task<FileContentResult> FileContent(string id, CancellationToken cancellationToken)
    {
        var fi = await (await bucket.FindAsync(gbf.Eq(c => c.Id, ObjectId.Parse(id)), cancellationToken: cancellationToken)).SingleOrDefaultAsync(cancellationToken) ?? throw new("no data find");
        var bytes = await bucket.DownloadAsBytesAsync(ObjectId.Parse(id), new() { Seekable = true }, cancellationToken);
        var content_type = fi.Metadata["contentType"].AsString;
        return string.IsNullOrWhiteSpace(content_type)
                   ? throw new("The file ContentType cannot be determined, please confirm whether the file type is specified when uploading the file, or view it after downloaded.")
                   : File(bytes, content_type, fi.Filename);
    }

    /// <summary>
    /// 通过文件名打开文件
    /// </summary>
    /// <param name="name"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("FileContentByName/{name}")]
    public virtual async Task<FileContentResult> FileContentByName(string name, CancellationToken cancellationToken)
    {
        var fi = await (await bucket.FindAsync(gbf.Eq(c => c.Filename, name), cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken) ?? throw new("can't find this file");
        var bytes = await bucket.DownloadAsBytesByNameAsync(name, new() { Seekable = true }, cancellationToken);
        var content_type = fi.Metadata["contentType"].AsString;
        return string.IsNullOrWhiteSpace(content_type)
                   ? throw new("The file ContentType cannot be determined, please confirm whether the file type is specified when uploading the file, or view it after downloaded.")
                   : File(bytes, content_type, fi.Filename);
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
    public virtual async Task Rename(string id, string newName, CancellationToken cancellationToken)
    {
        await bucket.RenameAsync(ObjectId.Parse(id), newName, cancellationToken);
        await Coll.UpdateOneAsync(c => c.FileId == id, Builders<GridFSItemInfo>.Update.Set(c => c.FileName, newName), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="ids">文件ID集合</param>
    /// <returns></returns>
    [HttpDelete]
    public virtual async Task<IEnumerable<string>> Delete(CancellationToken cancellationToken, [FromBody] params string[] ids)
    {
        var oids = ids.Select(ObjectId.Parse).ToList();
        var fi = await (await bucket.FindAsync(gbf.In(c => c.Id, oids), cancellationToken: cancellationToken)).ToListAsync(cancellationToken);
        var fids = fi.Select(c => new { Id = c.Id.ToString(), FileName = c.Filename }).ToArray();

        // 删除 GridFS 中的文件
        foreach (var item in fids)
        {
            await bucket.DeleteAsync(ObjectId.Parse(item.Id), cancellationToken);
        }

        // 删除元数据集合中的记录
        await Coll.DeleteManyAsync(c => ids.AsEnumerable().Contains(c.FileId), cancellationToken);
        return fids.Select(c => c.FileName);
    }
}
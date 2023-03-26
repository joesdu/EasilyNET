using HEasilyNET.Core;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.GridFS;

/// <summary>
/// GridFS控制器,当引入Extension后,请使用Extension版本的API
/// </summary>
[ApiController, Route("api/[controller]")]
public class GridFSController : ControllerBase
{
    /// <summary>
    /// 查询过滤器
    /// </summary>
    private readonly FilterDefinitionBuilder<GridFSItemInfo> _bf = Builders<GridFSItemInfo>.Filter;

    /// <summary>
    /// GridFSBucket
    /// </summary>
    protected readonly GridFSBucket Bucket;

    /// <summary>
    /// IMongoCollection
    /// </summary>
    protected readonly IMongoCollection<GridFSItemInfo> Coll;

    /// <summary>
    /// GridFSFileInfo Filter
    /// </summary>
    protected readonly FilterDefinitionBuilder<GridFSFileInfo> gbf = Builders<GridFSFileInfo>.Filter;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="bucket"></param>
    /// <param name="collection"></param>
    public GridFSController(GridFSBucket bucket, IMongoCollection<GridFSItemInfo> collection)
    {
        Bucket = bucket;
        Coll = collection;
    }

    /// <summary>
    /// 获取已上传文件列表
    /// </summary>
    /// <param name="info">关键字支持:文件名,用户名,用户ID,App名称以及业务名称模糊匹配</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("Infos")]
    public virtual async Task<object> Infos(InfoSearch info, CancellationToken cancellationToken)
    {
        var f = _bf.Empty;
        if (!string.IsNullOrWhiteSpace(info.FileName)) f &= _bf.Where(c => c.FileName.Contains(info.FileName));
        if (!string.IsNullOrWhiteSpace(info.UserName)) f &= _bf.Where(c => c.UserName.Contains(info.UserName));
        if (!string.IsNullOrWhiteSpace(info.UserId)) f &= _bf.Where(c => c.UserId.Contains(info.UserId));
        if (!string.IsNullOrWhiteSpace(info.App)) f &= _bf.Where(c => c.App.Contains(info.App));
        if (!string.IsNullOrWhiteSpace(info.BusinessType)) f &= _bf.Where(c => c.BusinessType.Contains(info.BusinessType));
        if (info.Start is not null) f &= _bf.Gte(c => c.CreateTime, info.Start);
        if (info.End is not null) f &= _bf.Lte(c => c.CreateTime, info.End);
        if (!string.IsNullOrWhiteSpace(info.Key))
            f &= _bf.Or(_bf.Where(c => c.FileName.Contains(info.Key!)),
                _bf.Where(c => c.UserName.Contains(info.Key!)),
                _bf.Where(c => c.UserId.Contains(info.Key!)),
                _bf.Where(c => c.App.Contains(info.Key!)),
                _bf.Where(c => c.BusinessType.Contains(info.Key!)));
        var total = await Coll.CountDocumentsAsync(f, cancellationToken: cancellationToken);
        var list = await Coll
                         .FindAsync(f, new()
                         {
                             Sort = Builders<GridFSItemInfo>.Sort.Descending(c => c.CreateTime),
                             Limit = info.PageSize,
                             Skip = info.Skip
                         }, cancellationToken)
                         .GetAwaiter().GetResult().ToListAsync(cancellationToken);
        return PageResult.Wrap(total, list);
    }

    /// <summary>
    /// 添加一个或多个文件
    /// </summary>
    [HttpPost("UploadMulti")]
    public virtual async Task<IEnumerable<GridFSItem>> PostMulti([FromForm] UploadGridFSMulti fs, CancellationToken cancellationToken)
    {
        if (fs.File is null || fs.File.Count == 0) throw new("no files find");
        if (fs.DeleteIds.Count > 0) _ = Delete(cancellationToken, fs.DeleteIds.ToArray());
        var rsList = new List<GridFSItem>();
        var infos = new List<GridFSItemInfo>();
        foreach (var item in fs.File)
        {
            if (item.ContentType is null) throw new("ContentType in File is null");
            var bapp = !string.IsNullOrWhiteSpace(fs.App) ? fs.App : GridFSExtensions.BusinessApp;
            if (string.IsNullOrWhiteSpace(bapp)) throw new("BusinessApp can't be null");
            var metadata = new Dictionary<string, object>
            {
                { "contentType", item.ContentType }, { "app", bapp }, { "creator", new { fs.UserId, fs.UserName }.ToBsonDocument() }
            };
            if (!string.IsNullOrWhiteSpace(fs.BusinessType)) metadata.Add("business", fs.BusinessType);
            if (!string.IsNullOrWhiteSpace(fs.CategoryId)) metadata.Add("category", fs.CategoryId!);
            var upo = new GridFSUploadOptions { BatchSize = fs.File.Count, Metadata = new(metadata) };
            var oid = await Bucket.UploadFromStreamAsync(item.FileName, item.OpenReadStream(), upo, cancellationToken);
            rsList.Add(new() { FileId = oid.ToString(), FileName = item.FileName, Length = item.Length, ContentType = item.ContentType });
            infos.Add(new()
            {
                FileId = oid.ToString(),
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
        }
        _ = Coll.InsertManyAsync(infos, cancellationToken: cancellationToken);
        return rsList;
    }

    /// <summary>
    /// 添加一个文件
    /// </summary>
    [HttpPost("UploadSingle")]
    public virtual async Task<GridFSItem> PostSingle([FromForm] UploadGridFSSingle fs, CancellationToken cancellationToken)
    {
        if (fs.File is null) throw new("no files find");
        if (!string.IsNullOrWhiteSpace(fs.DeleteId)) _ = await Delete(cancellationToken, fs.DeleteId!);
        if (fs.File.ContentType is null) throw new("ContentType in File is null");
        var bapp = !string.IsNullOrWhiteSpace(fs.App) ? fs.App : GridFSExtensions.BusinessApp;
        if (string.IsNullOrWhiteSpace(bapp)) throw new("BusinessApp can't be null");
        var metadata = new Dictionary<string, object>
        {
            { "contentType", fs.File.ContentType }, { "app", bapp }, { "creator", new { fs.UserId, fs.UserName }.ToBsonDocument() }
        };
        if (!string.IsNullOrWhiteSpace(fs.BusinessType)) metadata.Add("business", fs.BusinessType);
        if (!string.IsNullOrWhiteSpace(fs.CategoryId)) metadata.Add("category", fs.CategoryId!);
        var upo = new GridFSUploadOptions { BatchSize = 1, Metadata = new(metadata) };
        var oid = await Bucket.UploadFromStreamAsync(fs.File.FileName, fs.File.OpenReadStream(), upo, cancellationToken);
        _ = Coll.InsertOneAsync(new()
        {
            FileId = oid.ToString(),
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
        return new() { FileId = oid.ToString(), FileName = fs.File.FileName, Length = fs.File.Length, ContentType = fs.File.ContentType };
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
        var stream = await Bucket.OpenDownloadStreamAsync(ObjectId.Parse(id), new() { Seekable = true }, cancellationToken);
        var content_type = stream.FileInfo.Metadata["contentType"].AsString;
        if (string.IsNullOrWhiteSpace(content_type)) content_type = "application/octet-stream";
        return File(stream, content_type, stream.FileInfo.Filename);
    }

    /// <summary>
    /// 通过文件名下载
    /// </summary>
    /// <param name="name"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("DownloadByName/{name}")]
    public virtual async Task<FileStreamResult> DownloadByName(string name, CancellationToken cancellationToken)
    {
        var stream = await Bucket.OpenDownloadStreamByNameAsync(name, new() { Seekable = true }, cancellationToken);
        var content_type = stream.FileInfo.Metadata["contentType"].AsString;
        if (string.IsNullOrWhiteSpace(content_type)) content_type = "application/octet-stream";
        return File(stream, content_type, stream.FileInfo.Filename);
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
        var fi = await (await Bucket.FindAsync(gbf.Eq(c => c.Id, ObjectId.Parse(id)), cancellationToken: cancellationToken)).SingleOrDefaultAsync(cancellationToken) ?? throw new("no data find");
        var bytes = await Bucket.DownloadAsBytesAsync(ObjectId.Parse(id), new() { Seekable = true }, cancellationToken);
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
        var fi = await (await Bucket.FindAsync(gbf.Eq(c => c.Filename, name), cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken) ?? throw new("can't find this file");
        var bytes = await Bucket.DownloadAsBytesByNameAsync(name, new() { Seekable = true }, cancellationToken);
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
    public virtual Task Rename(string id, string newName, CancellationToken cancellationToken)
    {
        _ = Bucket.RenameAsync(ObjectId.Parse(id), newName, cancellationToken);
        _ = Coll.UpdateOneAsync(c => c.FileId == id, Builders<GridFSItemInfo>.Update.Set(c => c.FileName, newName), cancellationToken: cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="ids">文件ID集合</param>
    /// <returns></returns>
    [HttpDelete]
    public virtual async Task<IEnumerable<string>> Delete(CancellationToken cancellationToken, params string[] ids)
    {
        var oids = ids.Select(ObjectId.Parse).ToList();
        var fi = await (await Bucket.FindAsync(gbf.In(c => c.Id, oids), cancellationToken: cancellationToken)).ToListAsync(cancellationToken);
        var fids = fi.Select(c => new { Id = c.Id.ToString(), FileName = c.Filename }).ToArray();

        Task DeleteSingleFile()
        {
            foreach (var item in fids) _ = Bucket.DeleteAsync(ObjectId.Parse(item.Id), cancellationToken);
            return Task.CompletedTask;
        }

        _ = fids.Length > 6 ? Task.Run(DeleteSingleFile, cancellationToken) : DeleteSingleFile();
        _ = Coll.DeleteManyAsync(c => ids.Contains(c.FileId), cancellationToken);
        return fids.Select(c => c.FileName);
    }
}
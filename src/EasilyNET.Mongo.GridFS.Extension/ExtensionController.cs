using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.GridFS.Extension;

/// <summary>
/// GriFS扩展控制器
/// </summary>
[ApiController, Route("api/[controller]")]
public class ExtensionController : GridFSController
{
    private readonly EasilyFSSettings FileSetting;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="bucket"></param>
    /// <param name="collection"></param>
    /// <param name="config"></param>
    public ExtensionController(GridFSBucket bucket, IMongoCollection<GridFSItemInfo> collection, IConfiguration config) : base(bucket, collection)
    {
        FileSetting = new()
        {
            VirtualPath = config[$"{EasilyFSSettings.Position}:VirtualPath"],
            PhysicalPath = config[$"{EasilyFSSettings.Position}:PhysicalPath"]
        };
    }

    /// <summary>
    /// 获取虚拟目录的文件路径
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("FileUri/{id}")]
    public async Task<object> FileUri(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(FileSetting.PhysicalPath)) throw new("RealPath is null");
        var fi = await Coll.Find(_bf.Eq(c => c.FileId, id)).SingleOrDefaultAsync(cancellationToken) ?? throw new("no data find");
        // ReSharper disable once UseAwaitUsing
        using var mongoStream = await Bucket.OpenDownloadStreamAsync(ObjectId.Parse(id), new() { Seekable = true }, cancellationToken);
        if (!Directory.Exists(FileSetting.PhysicalPath)) _ = Directory.CreateDirectory(FileSetting.PhysicalPath);
        // ReSharper disable once UseAwaitUsing
        using var fsWrite = new FileStream($"{FileSetting.PhysicalPath}{Path.DirectorySeparatorChar}{fi.FileName}", FileMode.Create);
        // 来个1MB缓冲区
        var buffer = new byte[1024 * 1024 * 1];
        while (true)
        {
            var readCount = await mongoStream.ReadAsync(buffer, cancellationToken);
            await fsWrite.WriteAsync(buffer.AsMemory(0, readCount), cancellationToken);
            if (readCount < buffer.Length) break;
        }
        return new { Uri = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{FileSetting.VirtualPath}/{fi.FileName}" };
    }

    /// <summary>
    /// 清理缓存文件夹
    /// </summary>
    /// <returns></returns>
    [HttpDelete("ClearTempDir")]
    public Task ClearDir()
    {
        if (!Directory.Exists(FileSetting.PhysicalPath)) return Task.CompletedTask;
        try
        {
            Directory.Delete(FileSetting.PhysicalPath, true);
        }
        catch (IOException e)
        {
            Console.WriteLine(e.Message);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 重命名文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="newName">新名称</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id}/Rename/{newName}")]
    public override Task Rename(string id, string newName, CancellationToken cancellationToken)
    {
        var filename = Coll.Find(c => c.FileId == id).Project(c => c.FileName).SingleOrDefaultAsync(cancellationToken).GetAwaiter().GetResult();
        var path = $"{FileSetting.PhysicalPath}{Path.DirectorySeparatorChar}{filename}";
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        _ = base.Rename(id, newName, cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="ids">文件ID集合</param>
    /// <returns></returns>
    [HttpDelete]
    public override async Task<IEnumerable<string>> Delete(CancellationToken cancellationToken, params string[] ids)
    {
        var files = (await base.Delete(cancellationToken, ids)).ToList();

        Task DeleteSingleFile()
        {
            foreach (var path in files.Select(item => $"{FileSetting.PhysicalPath}{Path.DirectorySeparatorChar}{item}").Where(System.IO.File.Exists))
                System.IO.File.Delete(path);
            return Task.CompletedTask;
        }

        _ = files.Count > 6 ? Task.Run(DeleteSingleFile, cancellationToken) : DeleteSingleFile();
        return files;
    }
}
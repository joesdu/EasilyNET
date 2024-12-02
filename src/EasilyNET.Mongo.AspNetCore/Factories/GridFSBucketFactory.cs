using EasilyNET.Mongo.AspNetCore.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace EasilyNET.Mongo.AspNetCore.Factories;

/// <summary>
/// GridFSBucketFactory
/// </summary>
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="optionsMonitor"></param>
internal sealed class GridFSBucketFactory(IOptionsMonitor<GridFSBucketOptions> optionsMonitor) : IGridFSBucketFactory
{
    /// <summary>
    /// 创建客户端
    /// </summary>
    /// <returns></returns>
    public IGridFSBucket CreateBucket(IMongoDatabase db)
    {
        var options = optionsMonitor.Get(Constant.ConfigName);
        return new GridFSBucket(db, options);
    }
}
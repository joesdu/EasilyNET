using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace EasilyNET.MongoGridFS.AspNetCore;

/// <summary>
/// 工厂接口
/// </summary>
internal interface IGridFSBucketFactory
{
    /// <summary>
    /// 创建客户端
    /// </summary>
    /// <param name="db">Mongo数据库</param>
    /// <returns></returns>
    IGridFSBucket CreateBucket(IMongoDatabase db);
}

using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace EasilyNET.Mongo.AspNetCore.Abstraction;

/// <summary>
///     <para xml:lang="en">Factory interface</para>
///     <para xml:lang="zh">工厂接口</para>
/// </summary>
internal interface IGridFSBucketFactory
{
    /// <summary>
    ///     <para xml:lang="en">Create client</para>
    ///     <para xml:lang="zh">创建客户端</para>
    /// </summary>
    /// <param name="db">
    ///     <para xml:lang="en">Mongo database</para>
    ///     <para xml:lang="zh">Mongo数据库</para>
    /// </param>
    IGridFSBucket CreateBucket(IMongoDatabase db);
}
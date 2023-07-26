using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EasilyNET.MongoDistributedLock.Core;

/// <summary>
/// 释放信号
/// </summary>
public sealed class ReleaseSignal
{
    /// <summary>
    /// 锁ID
    /// </summary>
    [BsonId]
    public ObjectId AcquireId { get; set; }
}
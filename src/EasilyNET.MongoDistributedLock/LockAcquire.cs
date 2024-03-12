using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.MongoDistributedLock;

/// <summary>
/// 锁定实体
/// </summary>
public sealed class LockAcquire
{
    /// <summary>
    /// Id
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresIn { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Acquired { get; set; }

    /// <summary>
    /// 锁ID
    /// </summary>
    public ObjectId AcquireId { get; set; }
}

using EasilyNET.MongoDistributedLock.Attributes;
using MongoDB.Bson;

namespace EasilyNET.MongoDistributedLock;

/// <inheritdoc />
internal sealed class Acquire : IAcquire
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="acquireId"></param>
    public Acquire(ObjectId acquireId)
    {
        Acquired = true;
        AcquireId = acquireId;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public Acquire()
    {
        Acquired = false;
    }

    /// <inheritdoc />
    public bool Acquired { get; }

    /// <inheritdoc />
    public ObjectId AcquireId { get; }
}
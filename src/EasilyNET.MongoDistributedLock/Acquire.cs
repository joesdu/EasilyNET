using EasilyNET.MongoDistributedLock.Attributes;
using MongoDB.Bson;

namespace EasilyNET.MongoDistributedLock;

/// <inheritdoc />
internal sealed class Acquire : IAcquire
{
    private readonly IDistributedLock? _distributedLock;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="acquireId"></param>
    /// <param name="distributedLock"></param>
    public Acquire(ObjectId acquireId, IDistributedLock distributedLock)
    {
        Acquired = true;
        AcquireId = acquireId;
        _distributedLock = distributedLock;
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

    public async ValueTask DisposeAsync()
    {
        if (Acquired && _distributedLock is not null)
        {
            await _distributedLock.ReleaseAsync(this);
        }
    }
}
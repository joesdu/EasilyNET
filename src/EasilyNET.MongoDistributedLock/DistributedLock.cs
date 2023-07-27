using EasilyNET.MongoDistributedLock.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.MongoDistributedLock;

/// <summary>
/// 使用锁和信号集合以及锁标识符初始化 DistributedLock 类的新实例
/// </summary>
public sealed class DistributedLock(IMongoCollection<LockAcquire> locks, IMongoCollection<ReleaseSignal> signals, ObjectId lockId) : IDistributedLock
{
    private readonly FilterDefinitionBuilder<LockAcquire> bf = Builders<LockAcquire>.Filter;
    private readonly UpdateDefinitionBuilder<LockAcquire> bu = Builders<LockAcquire>.Update;

    /// <inheritdoc />
    public async Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout)
    {
        if (lifetime < TimeSpan.Zero || lifetime > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(lifetime), "生存期的值(以毫秒为单位)为负数或大于最大值");
        if (timeout < TimeSpan.Zero || timeout > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(timeout), "超时值(以毫秒为单位)为负或大于最大值");
        var acquireId = ObjectId.GenerateNewId();
        while (await TryUpdateAsync(lifetime, acquireId) == false)
        {
            var acquire = await locks.Find(bf.Eq(x => x.Id, lockId)).FirstOrDefaultAsync();
            if (acquire is not null && await WaitSignalAsync(acquire.AcquireId, timeout) == false)
            {
                return await TryUpdateAsync(lifetime, acquireId) ? new Acquire(acquireId) : new();
            }
        }
        return new Acquire(acquireId);
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(IAcquire acquire)
    {
        ArgumentNullException.ThrowIfNull(acquire, nameof(acquire));
        if (!acquire.Acquired) return;
        var result = await locks.UpdateOneAsync(bf.Eq(x => x.Id, lockId) & bf.Eq(x => x.AcquireId, acquire.AcquireId), bu.Set(x => x.Acquired, false));
        if (result.IsAcknowledged && result.ModifiedCount > 0)
            await signals.InsertOneAsync(new() { AcquireId = acquire.AcquireId });
    }

    private async Task<bool> WaitSignalAsync(ObjectId acquireId, TimeSpan timeout)
    {
        using var cursor = await signals.Find(x => x.AcquireId == acquireId, new() { MaxAwaitTime = timeout, CursorType = CursorType.TailableAwait }).ToCursorAsync();
        var started = DateTime.UtcNow;
        while (await cursor.MoveNextAsync())
        {
            if (cursor.Current.Any()) return true;
            if (DateTime.UtcNow - started >= timeout) return false;
        }
        return false;
    }

    private async Task<bool> TryUpdateAsync(TimeSpan lifetime, ObjectId acquireId)
    {
        try
        {
            var filter = bf.Eq(x => x.Id, lockId) & bf.Or(bf.Eq(x => x.Acquired, false), bf.Lte(x => x.ExpiresIn, DateTime.UtcNow));
            var result = await locks.UpdateOneAsync(filter, bu.Set(x => x.Acquired, true)
                                                              .Set(x => x.ExpiresIn, DateTime.UtcNow + lifetime)
                                                              .Set(x => x.AcquireId, acquireId)
                                                              .SetOnInsert(x => x.Id, lockId), new() { IsUpsert = true });
            return result.IsAcknowledged;
        }
        catch (MongoWriteException ex) // E11000 
        {
            if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                return false;
            throw;
        }
    }
}
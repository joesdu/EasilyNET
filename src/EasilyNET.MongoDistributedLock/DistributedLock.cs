using EasilyNET.MongoDistributedLock.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.MongoDistributedLock;

/// <summary>
/// 使用锁和信号集合以及锁标识符初始化 DistributedLock 类的新实例
/// </summary>
public sealed class DistributedLock : IDistributedLock
{
    private readonly ObjectId _lockId;

    private readonly IMongoCollection<LockAcquire> _locks;
    private readonly IMongoCollection<ReleaseSignal> _signals;
    private readonly FilterDefinitionBuilder<LockAcquire> bf = Builders<LockAcquire>.Filter;
    private readonly UpdateDefinitionBuilder<LockAcquire> bu = Builders<LockAcquire>.Update;

    private DistributedLock(IMongoCollection<LockAcquire> locks, IMongoCollection<ReleaseSignal> signals, ObjectId lockId)
    {
        _locks = locks;
        _signals = signals;
        _lockId = lockId;
    }

    /// <inheritdoc />
    public async Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout)
    {
        if (lifetime < TimeSpan.Zero || lifetime > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(lifetime), "生存期的值(以毫秒为单位)为负数或大于最大值");
        if (timeout < TimeSpan.Zero || timeout > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(timeout), "超时值(以毫秒为单位)为负或大于最大值");
        var acquireId = ObjectId.GenerateNewId();
        while (await TryUpdateAsync(lifetime, acquireId) == false)
        {
            var acquire = await _locks.Find(bf.Eq(c => c.Id, _lockId)).FirstOrDefaultAsync();
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
        var result = await _locks.UpdateOneAsync(bf.Eq(c => c.Id, _lockId) & bf.Eq(c => c.AcquireId, acquire.AcquireId), bu.Set(c => c.Acquired, false));
        if (result.IsAcknowledged && result.ModifiedCount > 0)
            await _signals.InsertOneAsync(new() { AcquireId = acquire.AcquireId });
    }

    /// <summary>
    /// 获取一个新的锁
    /// </summary>
    /// <param name="locks"></param>
    /// <param name="signals"></param>
    /// <param name="lockId"></param>
    /// <returns></returns>
    public static IDistributedLock GenerateNew(IMongoCollection<LockAcquire> locks, IMongoCollection<ReleaseSignal> signals, ObjectId lockId) => new DistributedLock(locks, signals, lockId);

    private async Task<bool> WaitSignalAsync(ObjectId acquireId, TimeSpan timeout)
    {
        using var cursor = await _signals.Find(c => c.AcquireId == acquireId, new() { MaxAwaitTime = timeout, CursorType = CursorType.TailableAwait }).ToCursorAsync();
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
            var filter = bf.Eq(c => c.Id, _lockId) & bf.Or(bf.Eq(c => c.Acquired, false), bf.Lte(c => c.ExpiresIn, DateTime.UtcNow));
            var result = await _locks.UpdateOneAsync(filter, bu.Set(c => c.Acquired, true)
                                                               .Set(c => c.ExpiresIn, DateTime.UtcNow + lifetime)
                                                               .Set(c => c.AcquireId, acquireId)
                                                               .SetOnInsert(c => c.Id, _lockId), new() { IsUpsert = true });
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

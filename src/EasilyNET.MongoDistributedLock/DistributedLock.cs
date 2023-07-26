using EasilyNET.MongoDistributedLock.Core.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.MongoDistributedLock.Core;

/// <summary>
/// Initializes a new instance of the MongoLock class with a locks and signals collections and lock identifier
/// </summary>
/// <param name="lock">Identifier of exclusive lock</param>
public sealed class DistributedLock(IMongoDatabase db, ObjectId @lock) : IDistributedLock
{
    private readonly IMongoCollection<LockAcquire> _locks = db.GetCollection<LockAcquire>("lock.acquire");
    private readonly IMongoCollection<ReleaseSignal> _signals = db.GetCollection<ReleaseSignal>("lock.release.signal");
    private readonly FilterDefinitionBuilder<LockAcquire> bf = Builders<LockAcquire>.Filter;
    private readonly UpdateDefinitionBuilder<LockAcquire> bu = Builders<LockAcquire>.Update;

    /// <inheritdoc />
    public async Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout)
    {
        if (lifetime < TimeSpan.Zero || lifetime > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(lifetime), "The value of lifetime in milliseconds is negative or is greater than MaxValue");
        if (timeout < TimeSpan.Zero || timeout > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(timeout), "The value of timeout in milliseconds is negative or is greater than MaxValue");
        var acquireId = ObjectId.GenerateNewId();
        while (await TryUpdateAsync(lifetime, acquireId) == false)
        {
            var acquire = await _locks.Find(bf.Eq(x => x.Id, @lock)).FirstOrDefaultAsync();
            if (acquire != null && await WaitSignalAsync(acquire.AcquireId, timeout) == false)
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
        var result = await _locks.UpdateOneAsync(bf.Eq(x => x.Id, @lock) & bf.Eq(x => x.AcquireId, acquire.AcquireId), bu.Set(x => x.Acquired, false));
        if (result.IsAcknowledged && result.ModifiedCount > 0)
            await _signals.InsertOneAsync(new() { AcquireId = acquire.AcquireId });
    }

    /// <inheritdoc />
    public IAcquire Acquire(TimeSpan lifetime, TimeSpan timeout)
    {
        if (lifetime < TimeSpan.Zero || lifetime > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(lifetime), "The value of lifetime in milliseconds is negative or is greater than MaxValue");
        if (timeout < TimeSpan.Zero || timeout > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(timeout), "The value of timeout in milliseconds is negative or is greater than MaxValue");
        var acquireId = ObjectId.GenerateNewId();
        while (TryUpdate(lifetime, acquireId) == false)
        {
            var acquire = _locks.Find(bf.Eq(x => x.Id, @lock)).FirstOrDefault();
            if (acquire != null && WaitSignal(acquire.AcquireId, timeout) == false)
            {
                return TryUpdate(lifetime, acquireId) ? new Acquire(acquireId) : new();
            }
        }
        return new Acquire(acquireId);
    }

    /// <inheritdoc />
    public void Release(IAcquire acquire)
    {
        ArgumentNullException.ThrowIfNull(acquire, nameof(acquire));
        if (!acquire.Acquired) return;
        var result = _locks.UpdateOne(bf.Eq(x => x.Id, @lock) & bf.Eq(x => x.AcquireId, acquire.AcquireId), bu.Set(x => x.Acquired, false));
        if (result.IsAcknowledged && result.ModifiedCount > 0)
            _signals.InsertOne(new() { AcquireId = acquire.AcquireId });
    }

    private async Task<bool> WaitSignalAsync(ObjectId acquireId, TimeSpan timeout)
    {
        using var cursor = await _signals.Find(x => x.AcquireId == acquireId, new() { MaxAwaitTime = timeout, CursorType = CursorType.TailableAwait }).ToCursorAsync();
        var started = DateTime.UtcNow;
        while (await cursor.MoveNextAsync())
        {
            if (cursor.Current.Any()) return true;
            if (DateTime.UtcNow - started >= timeout) return false;
        }
        return false;
    }

    private bool WaitSignal(ObjectId acquireId, TimeSpan timeout)
    {
        using var cursor = _signals.Find(x => x.AcquireId == acquireId, new() { MaxAwaitTime = timeout, CursorType = CursorType.TailableAwait }).ToCursor();
        var started = DateTime.UtcNow;
        while (cursor.MoveNext())
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
            var filter = bf.Eq(x => x.Id, @lock) & bf.Or(bf.Eq(x => x.Acquired, false) & bf.Lte(x => x.ExpiresIn, DateTime.UtcNow));
            var result = await _locks.UpdateOneAsync(filter, bu.Set(x => x.Acquired, true)
                                                               .Set(x => x.ExpiresIn, DateTime.UtcNow + lifetime)
                                                               .Set(x => x.AcquireId, acquireId)
                                                               .SetOnInsert(x => x.Id, @lock), new() { IsUpsert = true });
            return result.IsAcknowledged;
        }
        catch (MongoWriteException ex) // E11000 
        {
            if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                return false;
            throw;
        }
    }

    private bool TryUpdate(TimeSpan lifetime, ObjectId acquireId)
    {
        try
        {
            var filter = bf.Eq(x => x.Id, @lock) & bf.Or(bf.Eq(x => x.Acquired, false) & bf.Lte(x => x.ExpiresIn, DateTime.UtcNow));
            var result = _locks.UpdateOne(filter, bu
                                                  .Set(x => x.Acquired, true)
                                                  .Set(x => x.ExpiresIn, DateTime.UtcNow + lifetime)
                                                  .Set(x => x.AcquireId, acquireId)
                                                  .SetOnInsert(x => x.Id, @lock), new() { IsUpsert = true });
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
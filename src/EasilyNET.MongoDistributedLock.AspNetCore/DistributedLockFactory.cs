using EasilyNET.MongoDistributedLock;
using Microsoft.Extensions.DependencyInjection.Abstraction;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Extensions.DependencyInjection;

/// <inheritdoc />
internal sealed class DistributedLockFactory : IDistributedLockFactory
{
    /// <inheritdoc />
    public DistributedLock CreateMongoLock(IMongoCollection<LockAcquire> locks, IMongoCollection<ReleaseSignal> signal) => new(locks, signal, ObjectId.GenerateNewId());
}
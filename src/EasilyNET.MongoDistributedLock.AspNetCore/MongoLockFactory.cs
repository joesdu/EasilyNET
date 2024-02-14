using EasilyNET.MongoDistributedLock;
using EasilyNET.MongoDistributedLock.Attributes;
using Microsoft.Extensions.DependencyInjection.Abstraction;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Extensions.DependencyInjection;

/// <inheritdoc />
public sealed class MongoLockFactory : IMongoLockFactory
{
    private MongoLockFactory(IMongoCollection<LockAcquire> locks, IMongoCollection<ReleaseSignal> signal)
    {
        Locks = locks;
        Signal = signal;
    }

    private IMongoCollection<LockAcquire> Locks { get; }

    private IMongoCollection<ReleaseSignal> Signal { get; }

    /// <inheritdoc />
    public IDistributedLock GenerateNewLock(ObjectId locks) => DistributedLock.GenerateNew(Locks, Signal, locks);

    internal static IMongoLockFactory Instance(IMongoCollection<LockAcquire> locks, IMongoCollection<ReleaseSignal> signal) => new MongoLockFactory(locks, signal);
}

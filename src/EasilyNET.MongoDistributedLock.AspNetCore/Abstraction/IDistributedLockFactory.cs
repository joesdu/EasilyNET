using EasilyNET.MongoDistributedLock;
using MongoDB.Driver;

namespace Microsoft.Extensions.DependencyInjection.Abstraction;

/// <summary>
/// 工厂接口
/// </summary>
internal interface IDistributedLockFactory
{
    /// <summary>
    /// 创建客户端
    /// </summary>
    /// <param name="locks"></param>
    /// <param name="signal"></param>
    /// <returns></returns>
    DistributedLock CreateMongoLock(IMongoCollection<LockAcquire> locks, IMongoCollection<ReleaseSignal> signal);
}
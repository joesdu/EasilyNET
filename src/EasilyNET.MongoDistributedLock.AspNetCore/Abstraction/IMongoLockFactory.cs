using EasilyNET.MongoDistributedLock.Attributes;
using MongoDB.Bson;

namespace Microsoft.Extensions.DependencyInjection.Abstraction;

/// <summary>
/// 工厂接口
/// </summary>
public interface IMongoLockFactory
{
    /// <summary>
    /// 创建客户端
    /// </summary>
    /// <param name="locks"></param>
    /// <returns></returns>
    IDistributedLock GenerateNewLock(ObjectId locks);
}
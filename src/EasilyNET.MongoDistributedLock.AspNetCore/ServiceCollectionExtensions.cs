using EasilyNET.MongoDistributedLock;
using EasilyNET.MongoDistributedLock.Attributes;
using Microsoft.Extensions.DependencyInjection.Abstraction;
using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务扩展类
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 从容器中注册的 <see cref="IMongoClient" /> 注册服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    public static void AddMongoDistributedLock(this IServiceCollection services, Action<LockOptions>? options = null)
    {
        var option = new LockOptions();
        options?.Invoke(option);
        services.AddSingleton<IDistributedLockFactory, DistributedLockFactory>();
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMongoClient>();
        services.AddMongoDistributedLock(client, options);
    }

    /// <summary>
    /// 使用 <see cref="IMongoClient" /> 注册服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="client"></param>
    /// <param name="options"></param>
    public static void AddMongoDistributedLock(this IServiceCollection services, IMongoClient client, Action<LockOptions>? options = null)
    {
        var option = new LockOptions();
        options?.Invoke(option);
        var db = client.GetDatabase(option.DatabaseName);
        services.AddMongoDistributedLock(db, options);
    }

    /// <summary>
    /// 使用 <see cref="IMongoDatabase" /> 注册服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="db"></param>
    /// <param name="options"></param>
    public static void AddMongoDistributedLock(this IServiceCollection services, IMongoDatabase db, Action<LockOptions>? options = null)
    {
        var option = new LockOptions();
        options?.Invoke(option);
        try
        {
            db.CreateCollection(option.SignalCollName, new()
            {
                MaxDocuments = option.MaxDocument,
                MaxSize = option.MaxSize,
                Capped = true
            });
        }
        catch
        {
            // ignored
        }
        var _locks = db.GetCollection<LockAcquire>(option.AcquireCollName);
        var _signals = db.GetCollection<ReleaseSignal>(option.SignalCollName);
        services.AddSingleton<IDistributedLockFactory, DistributedLockFactory>();
        services.AddTransient<IDistributedLock>(sp =>
        {
            var factory = sp.GetRequiredService<IDistributedLockFactory>();
            return factory.CreateMongoLock(_locks, _signals);
        });
    }
}
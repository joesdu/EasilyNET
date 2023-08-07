using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;

namespace WebApi.Test.Unit;

/// <summary>
/// 分布式锁
/// </summary>
public sealed class DistributedLockModule : AppModule
{
    /// <inheritdoc />
    public DistributedLockModule()
    {
        Enable = false;
    }

    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddMongoDistributedLock(op =>
        {
            op.DatabaseName = "test_locks";
            op.MaxDocument = 100;
        });
    }
}
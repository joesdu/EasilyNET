using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// 分布式锁
/// </summary>
internal sealed class DistributedLockModule : AppModule
{
    /// <inheritdoc />
    public DistributedLockModule()
    {
        Enable = true;
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
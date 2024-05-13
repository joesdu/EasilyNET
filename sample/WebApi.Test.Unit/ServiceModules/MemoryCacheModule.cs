using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;

namespace WebApi.Test.Unit;

/// <inheritdoc />
public sealed class MemoryCacheModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddDistributedMemoryCache();
        base.ConfigureServices(context);
    }
}
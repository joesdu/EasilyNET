using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using WebApi.Test.Unit.Common;

namespace WebApi.Test.Unit.ServiceModules;

/// <inheritdoc />
internal sealed class GarnetDistributedCacheModule : AppModule
{
    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var garnet = context.ServiceProvider.GetConfiguration().GetConnectionString("Garnet");
        context.Services.AddStackExchangeRedisCache(c =>
        {
            c.Configuration = garnet;
            c.InstanceName = Constant.InstanceName;
        });
        await base.ConfigureServices(context);
    }
}
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using WebApi.Test.Unit.Common;

namespace WebApi.Test.Unit.ServiceModules;

/// <inheritdoc />
internal sealed class GarnetDistributedCacheModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var garnet = context.Configuration.GetConnectionString("Garnet");
        context.Services.AddStackExchangeRedisCache(c =>
        {
            c.Configuration = garnet;
            c.InstanceName = Constant.InstanceName;
        });
    }
}
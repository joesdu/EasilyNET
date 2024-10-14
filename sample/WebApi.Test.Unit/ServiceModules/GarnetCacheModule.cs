using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using WebApi.Test.Unit.Common;

namespace WebApi.Test.Unit.ServiceModules;

/// <inheritdoc />
internal sealed class GarnetCacheModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var garnet = context.Services.GetConfiguration().GetConnectionString("Garnet");
        context.Services.AddStackExchangeRedisCache(c =>
        {
            c.Configuration = garnet;
            c.InstanceName = Constant.InstanceName;
        });
        base.ConfigureServices(context);
    }
}
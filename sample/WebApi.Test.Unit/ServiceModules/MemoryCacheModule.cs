using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using WebApi.Test.Unit.Common;

namespace WebApi.Test.Unit;

/// <inheritdoc />
internal sealed class MemoryCacheModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        context.Services.AddStackExchangeRedisCache(c =>
        {
            c.Configuration = config["CONNECTIONSTRINGS_GARNET"];
            c.InstanceName = Constant.InstanceName;
        });
        base.ConfigureServices(context);
    }
}
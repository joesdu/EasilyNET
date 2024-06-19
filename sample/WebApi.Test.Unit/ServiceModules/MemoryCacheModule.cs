using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;

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
            c.InstanceName = "EasilyNET";
        });
        base.ConfigureServices(context);
    }
}
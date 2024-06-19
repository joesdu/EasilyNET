using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;

namespace WebApi.Test.Unit;

/// <summary>
/// 缓存
/// </summary>
internal sealed class OutPutCachingModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        context.Services.AddStackExchangeRedisOutputCache(c =>
        {
            c.Configuration = config["CONNECTIONSTRINGS_GARNET"];
            c.InstanceName = "EasilyNET";
        });
    }

    /// <inheritdoc />
    public override void ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationBuilder();
        app.UseOutputCache();
    }
}
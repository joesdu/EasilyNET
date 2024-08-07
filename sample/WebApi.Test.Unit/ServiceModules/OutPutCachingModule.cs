using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using WebApi.Test.Unit.Common;

namespace WebApi.Test.Unit.ServiceModules;

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
            c.InstanceName = Constant.InstanceName;
        });
    }

    /// <inheritdoc />
    public override void ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseOutputCache();
    }
}
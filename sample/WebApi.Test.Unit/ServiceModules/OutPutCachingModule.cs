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
        var garnet = context.Services.GetConfiguration().GetConnectionString("Garnet");
        context.Services.AddStackExchangeRedisOutputCache(c =>
        {
            c.Configuration = garnet;
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
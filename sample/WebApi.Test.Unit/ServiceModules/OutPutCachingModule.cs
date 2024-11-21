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
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var garnet = context.ServiceProvider.GetConfiguration().GetConnectionString("Garnet");
        context.Services.AddStackExchangeRedisOutputCache(c =>
        {
            c.Configuration = garnet;
            c.InstanceName = Constant.InstanceName;
        });
        await base.ConfigureServices(context);
    }

    /// <inheritdoc />
    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseOutputCache();
        await base.ApplicationInitialization(context);
    }
}
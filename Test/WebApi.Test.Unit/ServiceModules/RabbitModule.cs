using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.RabbitBus.AspNetCore;

namespace WebApi.Test.Unit;

/// <summary>
/// Rabbit服务注册
/// </summary>
public class RabbitModule : AppModule
{
    /// <inheritdoc />
    public RabbitModule()
    {
        Enable = false;
    }

    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        context.Services.AddRabbitBus(config, max_channel_count: (uint)Environment.ProcessorCount);
    }
}
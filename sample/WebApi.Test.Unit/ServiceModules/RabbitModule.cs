using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;

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
        context.Services.AddRabbitBus(config, poolCount: (uint)Environment.ProcessorCount);
    }
}
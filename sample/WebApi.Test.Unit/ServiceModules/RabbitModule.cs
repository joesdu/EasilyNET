using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.RabbitBus.AspNetCore.Enums;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// Rabbit服务注册
/// </summary>
internal sealed class RabbitModule : AppModule
{
    /// <inheritdoc />
    public RabbitModule()
    {
        Enable = true;
    }

    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        context.Services.AddRabbitBus(config, c =>
        {
            c.PoolCount = (uint)Environment.ProcessorCount;
            c.RetryCount = 5;
            c.Serializer = ESerializer.TextJson;
        });
    }
}
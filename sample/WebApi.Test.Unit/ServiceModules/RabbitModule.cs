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
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();
        context.Services.AddRabbitBus(config, c =>
        {
            c.PoolCount = (uint)Environment.ProcessorCount;
            c.RetryCount = 5;
            c.Serializer = ESerializer.TextJson;
        });
        await Task.CompletedTask;
    }
}
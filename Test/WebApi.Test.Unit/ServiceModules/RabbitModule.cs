using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.RabbitBus;

namespace WebApi.Test.Unit;

/// <summary>
/// Rabbit服务注册
/// </summary>
public class RabbitModule : AppModule
{
    /// <summary>
    /// 配置服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        context.Services.AddRabbitBus(c =>
        {
            c.AmqpTcpEndpoints = new()
            {
                new("localhost")
            };
        });
    }
}
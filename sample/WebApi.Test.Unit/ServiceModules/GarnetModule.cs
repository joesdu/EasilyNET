using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using StackExchange.Redis;

namespace WebApi.Test.Unit.ServiceModules;

internal sealed class GarnetModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var garnet = context.Services.GetConfiguration().GetConnectionString("Garnet") ?? throw new("无法读取到连接字符串");
        var configurationOptions = ConfigurationOptions.Parse(garnet);
        configurationOptions.ClientName = "Garnet";
        var client = ConnectionMultiplexer.ConnectAsync(configurationOptions);
        context.Services.AddSingleton<IConnectionMultiplexer>(client.Result);
    }
}
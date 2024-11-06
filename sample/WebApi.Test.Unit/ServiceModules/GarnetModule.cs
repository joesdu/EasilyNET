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
        // Garnet 只有 0 数据库,所以这里直接注册 IDatabase
        context.Services.AddSingleton(_ => client.Result.GetDatabase(0));
    }
}
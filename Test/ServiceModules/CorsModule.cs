using EasilyNET.DependencyInjection.Contexts;
using EasilyNET.DependencyInjection.Extensions;
using EasilyNET.DependencyInjection.Modules;

namespace example.api;

/// <summary>
/// 配置跨域服务及中间件
/// </summary>
public class CorsModule : AppModule
{
    private CorsModule()
    {
        // 使模块在自动注入的时候忽略.
        Enable = !false;
    }
    /// <summary>
    /// 注册和配置服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        var allow = config["AllowedHosts"] ?? "*";
        _ = context.Services.AddCors(c => c.AddPolicy("AllowedHosts", s => s.WithOrigins(allow.Split(",")).AllowAnyMethod().AllowAnyHeader()));
    }
    /// <summary>
    /// 注册中间件
    /// </summary>
    /// <param name="context"></param>
    public override void ApplicationInitialization(ApplicationContext context)
    {
        Console.WriteLine(2);
        var app = context.GetApplicationBuilder();
        _ = app.UseCors("AllowedHosts");
    }
}
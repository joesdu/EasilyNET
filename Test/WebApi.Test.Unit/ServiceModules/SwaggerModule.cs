using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.WebCore.Swagger;
using Microsoft.OpenApi.Models;

namespace WebApi.Test.Unit;

/// <summary>
/// Swagger文档的配置
/// </summary>
public class SwaggerModule : AppModule
{
    /**
     * https://github.com/domaindrivendev/Swashbuckle.AspNetCore
     */
    private const string name = $"{title}-{version}";

    private const string version = "v1";
    private const string title = "WebApi.Test";

    /// <summary>
    /// 配置和注册服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        _ = context.Services.AddSwaggerGen(c =>
        {
            // 配置默认的文档信息
            c.SwaggerDoc(name, new()
            {
                Title = title,
                Version = version,
                Description = "Console.WriteLine(\"🐂🍺\")"
            });
            // 这里使用EasilyNET提供的扩展配置.
            c.EasilySwaggerGenOptions(name);
            // 配置认证方式
            c.AddSecurityDefinition("Bearer", new()
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
        });
    }

    /// <summary>
    /// 注册中间件
    /// </summary>
    /// <param name="context"></param>
    public override void ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationBuilder();
        _ = app.UseSwagger().UseSwaggerUI(c =>
        {
            // 配置默认文档
            c.SwaggerEndpoint($"/swagger/{name}/swagger.json", $"{title} {version}");
            // 使用EasilyNET提供的扩展配置
            c.EasilySwaggerUIOptions();
        });
    }
}
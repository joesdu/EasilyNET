using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// Swagger文档的配置
/// </summary>
internal sealed class SwaggerModule : AppModule
{
    /// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
    /// <inheritdoc />
    public override bool GetEnable(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();
        return config.GetSection("ServicesEnable").GetValue<bool>("Swagger");
    }

    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        context.Services.AddSwaggerGen(c =>
        {
            // 这里使用EasilyNET提供的扩展配置.
            c.EasilySwaggerGenOptions();
            // 配置认证方式
            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new()
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme
            });
        });
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseEasilySwaggerUI();
        await base.ApplicationInitialization(context);
    }
}
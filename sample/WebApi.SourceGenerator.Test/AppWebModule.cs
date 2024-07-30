using EasilyNET.AutoDependencyInjection.Attributes;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.WebCore.Handlers;
using WebApi.SourceGenerator.Test.ServiceModules;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.SourceGenerator.Test;

/// <summary>
/// </summary>
[DependsOn(typeof(DependencyAppModule),
    typeof(CorsModule),
    typeof(ControllersModule),
    typeof(SwaggerModule))]
public sealed class AppWebModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        base.ConfigureServices(context);
        // 添加 ProblemDetails 服务
        context.Services.AddProblemDetails();
        context.Services.AddExceptionHandler<BusinessExceptionHandler>();
        // 添加HttpContextAccessor
        context.Services.AddHttpContextAccessor();
    }

    /// <inheritdoc />
    public override void ApplicationInitialization(ApplicationContext context)
    {
        base.ApplicationInitialization(context);
        var app = context.GetApplicationHost() as IApplicationBuilder;
        // 全局异常处理中间件
        app?.UseExceptionHandler();
        app?.UseResponseTime();
        // 先认证
        app?.UseAuthentication();
        // 再授权
        app?.UseAuthorization();
    }
}
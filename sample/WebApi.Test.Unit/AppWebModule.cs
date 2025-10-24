using EasilyNET.AutoDependencyInjection.Attributes;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.WebCore.Handlers;
using WebApi.Test.Unit.ServiceModules;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit;

/**
 * 要实现自动注入,一定要在这个地方添加,由于中间件的注册顺序会对程序产生巨大影响,因此请注意模块的注入顺序,服务配置的顺序无所谓.
 * 该处模块注入顺序为从上至下,本类AppWebModule最先注册.所以本类中中间件注册函数ApplicationInitialization最先执行.
 */
[DependsOn(typeof(DependencyAppModule),
    typeof(ResponseCompressionModule),
    typeof(CorsModule),
    typeof(ControllersModule),
    typeof(GarnetDistributedCacheModule),
    typeof(MongoModule),
    typeof(MongoFSModule),
    typeof(RabbitModule),
    typeof(SwaggerModule),
    typeof(OpenTelemetryModule))]
internal sealed class AppWebModule : AppModule
{
    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        // 添加 ProblemDetails 服务
        context.Services.AddProblemDetails();
        context.Services.AddExceptionHandler<BusinessExceptionHandler>();
        // 添加HttpContextAccessor
        context.Services.AddHttpContextAccessor();
        await base.ConfigureServices(context);
    }

    /// <inheritdoc />
    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        // 全局异常处理中间件
        app?.UseExceptionHandler();
        app?.UseResponseTime();
        // 先认证
        app?.UseAuthentication();
        // 再授权
        app?.UseAuthorization();

        // 添加静态文件中间件，用于提供 wwwroot 中的文件
        app?.UseStaticFiles();
        await base.ApplicationInitialization(context);
    }
}
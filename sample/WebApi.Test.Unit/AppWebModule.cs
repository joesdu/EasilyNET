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
    typeof(RabbitModule),
    typeof(SwaggerModule),
    typeof(OpenTelemetryModule),
    typeof(WebSocketServerModule))]
internal sealed class AppWebModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        // 添加 ProblemDetails 服务
        context.Services.AddProblemDetails();
        context.Services.AddExceptionHandler<BusinessExceptionHandler>();
        // 添加HttpContextAccessor
        context.Services.AddHttpContextAccessor();
    }

    /// <inheritdoc />
    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        // 响应时间记录 - 放在最前以捕获所有中间件耗时
        app?.UseResponseTime();
        // 全局异常处理 - 尽早处理错误
        app?.UseExceptionHandler();
        // 静态文件 - 无需认证，可提前处理
        app?.UseStaticFiles();
        // 认证和授权
        app?.UseAuthentication();
        app?.UseAuthorization();
        await base.ApplicationInitialization(context);
    }
}
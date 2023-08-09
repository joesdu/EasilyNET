using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.AutoDependencyInjection.PropertyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Hosting;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务集合扩展
/// </summary>
public static partial class ServiceCollectionExtension
{
    /// <summary>
    /// 获取应用程序构建器
    /// </summary>
    /// <param name="applicationContext"></param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IApplicationBuilder GetApplicationBuilder(this ApplicationContext applicationContext) => applicationContext.ServiceProvider.GetRequiredService<IObjectAccessor<IApplicationBuilder>>().Value!;

    /// <summary>
    /// 注入服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplication<T>(this IServiceCollection services) where T : AppModule
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        var obj = new ObjectAccessor<IApplicationBuilder>();
        services.Add(ServiceDescriptor.Singleton(typeof(ObjectAccessor<IApplicationBuilder>), obj));
        services.Add(ServiceDescriptor.Singleton(typeof(IObjectAccessor<IApplicationBuilder>), obj));
        var runner = new StartupModuleRunner(typeof(T), services);
        runner.ConfigureServices(services);
        return services;
    }

    /// <summary>
    /// 初始化应用,配置中间件
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder InitializeApplication(this IApplicationBuilder builder)
    {
        builder.ApplicationServices.GetRequiredService<ObjectAccessor<IApplicationBuilder>>().Value = builder;
        var runner = builder.ApplicationServices.GetRequiredService<IStartupModuleRunner>();
        runner.Initialize(builder.ApplicationServices);
        return builder;
    }

    /// <summary>
    /// 使用属性注入
    /// </summary>
    /// <param name="hostBuilder">host构建器</param>
    /// <returns></returns>
    public static IHostBuilder UsePropertyInjection(this IHostBuilder hostBuilder)
    {
       return hostBuilder.UseServiceProviderFactory(new PropertyInjectionServiceProviderFactory()).ConfigureServices(ConfigureServices);
    }

    private static void ConfigureServices(IServiceCollection services) => services.AddSingleton<IPropertyInjector, PropertyInjector>().AddSingleton<IControllerFactory, PropertyInjectionControllerFactory>();
}
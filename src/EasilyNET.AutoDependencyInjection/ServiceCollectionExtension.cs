using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

// ReSharper disable UnusedMember.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection" /> 扩展
/// </summary>
public static partial class ServiceCollectionExtension
{
    /// <summary>
    /// 获取应用程序构建器
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static IApplicationBuilder GetApplicationBuilder(this ApplicationContext context) => context.ServiceProvider.GetRequiredService<IObjectAccessor<IApplicationBuilder>>().Value!;

    /// <summary>
    /// 注入服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplication<T>(this IServiceCollection services) where T : AppModule
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        services.AddSingleton<IObjectAccessor<IApplicationBuilder>>(new ObjectAccessor<IApplicationBuilder>());
        ApplicationFactory.Create<T>(services);
        return services;
    }

    /// <summary>
    /// 初始化应用,配置中间件
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder InitializeApplication(this IApplicationBuilder builder)
    {
        builder.ApplicationServices.GetRequiredService<IObjectAccessor<IApplicationBuilder>>().Value = builder;
        var runner = builder.ApplicationServices.GetRequiredService<IStartupModuleRunner>();
        runner.Initialize(builder.ApplicationServices);
        return builder;
    }

    /// <summary>
    /// 获取 <see cref="IConfiguration" /> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IConfiguration GetConfiguration(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IConfiguration>();
    }

    /// <summary>
    /// 获取 <see cref="IWebHostEnvironment" /> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IWebHostEnvironment GetWebHostEnvironment(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IWebHostEnvironment>();
    }

    /// <summary>
    /// 获取 <see cref="IConfiguration" /> 服务
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static IConfiguration GetConfiguration(this IServiceProvider provider) => provider.GetRequiredService<IConfiguration>();
}
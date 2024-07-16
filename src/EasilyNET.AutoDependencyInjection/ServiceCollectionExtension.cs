using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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
    public static IHost GetApplicationHost(this ApplicationContext context) => context.ServiceProvider.GetRequiredService<IObjectAccessor<IHost>>().Value!;

    /// <summary>
    /// 注入服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplicationModules<T>(this IServiceCollection services) where T : AppModule
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        services.AddSingleton<IObjectAccessor<IHost>>(new ObjectAccessor<IHost>());
        ApplicationFactory.Create<T>(services);
        return services;
    }

    /// <summary>
    /// 初始化应用,配置中间件
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static IHost InitializeApplication(this IHost host)
    {
        host.Services.GetRequiredService<IObjectAccessor<IHost>>().Value = host;
        var runner = host.Services.GetRequiredService<IStartupModuleRunner>();
        runner.Initialize();
        return host;
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
    /// 获取 <see cref="IConfiguration" /> 服务
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static IConfiguration GetConfiguration(this IServiceProvider provider) => provider.GetRequiredService<IConfiguration>();
}
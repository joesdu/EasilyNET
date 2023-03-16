using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection.Extensions;

/// <summary>
/// 应用模块扩展.
/// </summary>
public static class AppModuleExtensions
{
    /// <summary>
    /// 添加自动注入服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplication<T>(this IServiceCollection services) where T : AppModule
    {
#if NETSTANDARD
        if (services is null) throw new ArgumentNullException(nameof(services));
#else
        ArgumentNullException.ThrowIfNull(services, nameof(services));
#endif
        var obj = new ObjectAccessor<IApplicationBuilder>();
        services.Add(ServiceDescriptor.Singleton(typeof(ObjectAccessor<IApplicationBuilder>), obj));
        services.Add(ServiceDescriptor.Singleton(typeof(IObjectAccessor<IApplicationBuilder>), obj));
        var runner = new StartupModuleRunner(typeof(T), services);
        runner.ConfigureServices(services);
        return services;
    }

    /// <summary>
    /// 初始化应用
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
}
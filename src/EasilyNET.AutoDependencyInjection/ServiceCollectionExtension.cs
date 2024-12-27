using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Factories;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en"><see cref="IServiceCollection" /> extensions</para>
///     <para xml:lang="zh"><see cref="IServiceCollection" /> 扩展</para>
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    ///     <para xml:lang="en">Get the application host</para>
    ///     <para xml:lang="zh">获取应用程序构建器</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Application context</para>
    ///     <para xml:lang="zh">应用上下文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The application host</para>
    ///     <para xml:lang="zh">应用程序构建器</para>
    /// </returns>
    public static IHost GetApplicationHost(this ApplicationContext context) => context.ServiceProvider.GetRequiredService<IObjectAccessor<IHost>>().Value ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    ///     <para xml:lang="en">Inject services</para>
    ///     <para xml:lang="zh">注入服务</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">Type of the application module</para>
    ///     <para xml:lang="zh">应用模块的类型</para>
    /// </typeparam>
    /// <param name="services">
    ///     <para xml:lang="en"><see cref="IServiceCollection" /> to configure services</para>
    ///     <para xml:lang="zh">用于配置服务的 <see cref="IServiceCollection" /></para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </returns>
    public static IServiceCollection AddApplicationModules<T>(this IServiceCollection services) where T : AppModule
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        services.AddSingleton<IObjectAccessor<IHost>>(new ObjectAccessor<IHost>());
        ApplicationFactory.Create<T>(services);
        return services;
    }

    /// <summary>
    ///     <para xml:lang="en">Initialize the application and configure middleware</para>
    ///     <para xml:lang="zh">初始化应用，配置中间件</para>
    /// </summary>
    /// <param name="host">
    ///     <para xml:lang="en">The application host</para>
    ///     <para xml:lang="zh">应用程序构建器</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The initialized application host</para>
    ///     <para xml:lang="zh">已初始化的应用程序构建器</para>
    /// </returns>
    public static IHost InitializeApplication(this IHost host)
    {
        host.Services.GetRequiredService<IObjectAccessor<IHost>>().Value = host;
        var runner = host.Services.GetRequiredService<IStartupModuleRunner>();
        runner.Initialize();
        return host;
    }

    /// <summary>
    ///     <para xml:lang="en">Get the <see cref="IConfiguration" /> service</para>
    ///     <para xml:lang="zh">获取 <see cref="IConfiguration" /> 服务</para>
    /// </summary>
    /// <param name="provider">
    ///     <para xml:lang="en">Service provider</para>
    ///     <para xml:lang="zh">服务提供者</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The configuration service</para>
    ///     <para xml:lang="zh">配置服务</para>
    /// </returns>
    public static IConfiguration GetConfiguration(this IServiceProvider provider) => provider.GetRequiredService<IConfiguration>();
}
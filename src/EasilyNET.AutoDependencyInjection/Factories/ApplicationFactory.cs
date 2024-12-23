using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMethodReturnValue.Global

namespace EasilyNET.AutoDependencyInjection.Factories;

/// <summary>
///     <para xml:lang="en">Application factory</para>
///     <para xml:lang="zh">应用工厂</para>
/// </summary>
internal static class ApplicationFactory
{
    /// <summary>
    ///     <para xml:lang="en">Create an instance of <see cref="IStartupModuleRunner" /></para>
    ///     <para xml:lang="zh">创建 <see cref="IStartupModuleRunner" /> 的实例</para>
    /// </summary>
    /// <typeparam name="TStartupModule">
    ///     <para xml:lang="en">Type of the startup module</para>
    ///     <para xml:lang="zh">启动模块的类型</para>
    /// </typeparam>
    /// <param name="services">
    ///     <para xml:lang="en"><see cref="IServiceCollection" /> to configure services</para>
    ///     <para xml:lang="zh">用于配置服务的 <see cref="IServiceCollection" /></para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">An instance of <see cref="IStartupModuleRunner" /></para>
    ///     <para xml:lang="zh"><see cref="IStartupModuleRunner" /> 的实例</para>
    /// </returns>
    public static IStartupModuleRunner Create<TStartupModule>(IServiceCollection services) where TStartupModule : AppModule => StartupModuleRunner.Instance(typeof(TStartupModule), services);
}
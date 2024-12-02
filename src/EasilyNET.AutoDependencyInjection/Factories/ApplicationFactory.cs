using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMethodReturnValue.Global

namespace EasilyNET.AutoDependencyInjection.Factories;

/// <summary>
/// 应用工厂
/// </summary>
internal static class ApplicationFactory
{
    /// <summary>
    /// 创建
    /// </summary>
    /// <typeparam name="TStartupModule"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IStartupModuleRunner Create<TStartupModule>(IServiceCollection services) where TStartupModule : AppModule => StartupModuleRunner.Instance(typeof(TStartupModule), services);
}
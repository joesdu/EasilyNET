using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
/// 应用工厂
/// </summary>
public static class ApplicationFactory
{
    /// <summary>
    /// 创建
    /// </summary>
    /// <typeparam name="TStartupModule"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IStartupModuleRunner Create<TStartupModule>(IServiceCollection services) where TStartupModule : AppModule => new StartupModuleRunner(typeof(TStartupModule), services);
}

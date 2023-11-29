using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Modules;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
/// 应用工厂
/// </summary>
public static class ApplicationFactory
{
    /// <summary>
    /// 应用工厂创建
    /// </summary>
    /// <typeparam name="TStartupModule">启动模块</typeparam>
    /// <returns></returns>
    public static IApplicationWithInternalServiceProvider Create<TStartupModule>()
        where TStartupModule : AppModule =>
        new ApplicationWithInternalServiceProvider(typeof(TStartupModule));
}
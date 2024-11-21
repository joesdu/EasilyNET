using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// App模块接口
/// </summary>
public interface IAppModule : IApplicationInitialization
{
    /// <summary>
    /// 配置服务
    /// </summary>
    /// <param name="context"></param>
    Task ConfigureServices(ConfigureServicesContext context);

    /// <summary>
    /// 服务依赖集合
    /// </summary>
    /// <param name="moduleType"></param>
    /// <returns></returns>
    IEnumerable<Type> GetDependedTypes(Type? moduleType = null);

    /// <summary>
    /// 获取是否启用,从配置中获取
    /// </summary>
    /// <returns></returns>
    bool GetEnable(ConfigureServicesContext context);
}
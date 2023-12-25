using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <inheritdoc />
public interface IAppModule : IApplicationInitialization
{
    /// <summary>
    /// 是否启用
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    bool Enable { get; set; }

    /// <summary>
    /// 配置服务
    /// </summary>
    /// <param name="context"></param>
    void ConfigureServices(ConfigureServicesContext context);

    /// <summary>
    /// 服务依赖集合
    /// </summary>
    /// <param name="moduleType"></param>
    /// <returns></returns>
    IEnumerable<Type> GetDependedTypes(Type? moduleType = null);
}
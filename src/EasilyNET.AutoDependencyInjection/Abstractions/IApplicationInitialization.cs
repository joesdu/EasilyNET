using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// 应用初始化接口
/// </summary>
public interface IApplicationInitialization
{
    /// <summary>
    /// 应用初始化
    /// </summary>
    /// <param name="context"></param>
    void ApplicationInitialization(ApplicationContext context);
}
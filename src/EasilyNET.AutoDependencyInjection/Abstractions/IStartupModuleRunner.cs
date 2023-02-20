using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// 模块运行器
/// </summary>
internal interface IStartupModuleRunner : IModuleApplication
{
    /// <summary>
    /// 配置服务
    /// </summary>
    /// <param name="services"></param>
    void ConfigureServices(IServiceCollection services);
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="service"></param>
    void Initialize(IServiceProvider service);
}
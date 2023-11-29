namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// 模块运行器
/// </summary>
public interface IStartupModuleRunner : IModuleApplication
{
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="provider"></param>
    void Initialize(IServiceProvider? provider = null);
}
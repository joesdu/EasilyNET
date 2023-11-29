namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// </summary>
public interface IApplicationWithInternalServiceProvider : IModuleApplication
{
    /// <summary>
    /// 初始化
    /// </summary>
    void Initialize();
}
namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <inheritdoc />
public interface IStartupModuleRunner : IModuleApplication
{
    /// <summary>
    /// 初始化
    /// </summary>
    void Initialize();
}
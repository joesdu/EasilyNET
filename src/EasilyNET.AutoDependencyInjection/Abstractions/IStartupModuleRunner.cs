namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <inheritdoc />
internal interface IStartupModuleRunner : IModuleApplication
{
    /// <summary>
    /// 初始化
    /// </summary>
    void Initialize();
}
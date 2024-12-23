namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <inheritdoc />
internal interface IStartupModuleRunner : IModuleApplication
{
    /// <summary>
    ///     <para xml:lang="en">Initialize</para>
    ///     <para xml:lang="zh">初始化</para>
    /// </summary>
    void Initialize();
}
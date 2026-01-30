namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
///     <para xml:lang="en">Module diagnostics information</para>
///     <para xml:lang="zh">模块诊断信息</para>
/// </summary>
public sealed class ModuleInfo
{
    /// <summary>
    ///     <para xml:lang="en">Module type</para>
    ///     <para xml:lang="zh">模块类型</para>
    /// </summary>
    public required Type ModuleType { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Module name</para>
    ///     <para xml:lang="zh">模块名称</para>
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Execution order (0-based, lower executes first)</para>
    ///     <para xml:lang="zh">执行顺序（从0开始，数字越小越先执行）</para>
    /// </summary>
    public required int Order { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Direct dependencies of this module</para>
    ///     <para xml:lang="zh">此模块的直接依赖</para>
    /// </summary>
    public required IReadOnlyList<Type> Dependencies { get; init; }
}

/// <summary>
///     <para xml:lang="en">Service registration information for diagnostics</para>
///     <para xml:lang="zh">用于诊断的服务注册信息</para>
/// </summary>
public sealed class ServiceRegistrationInfo
{
    /// <summary>
    ///     <para xml:lang="en">Service type</para>
    ///     <para xml:lang="zh">服务类型</para>
    /// </summary>
    public required Type ServiceType { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Implementation type</para>
    ///     <para xml:lang="zh">实现类型</para>
    /// </summary>
    public required Type ImplementationType { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Service key (null for non-keyed services)</para>
    ///     <para xml:lang="zh">服务键（非键控服务为 null）</para>
    /// </summary>
    public object? ServiceKey { get; init; }
}

/// <summary>
///     <para xml:lang="en">Provides diagnostic information about loaded modules and registered services</para>
///     <para xml:lang="zh">提供有关已加载模块和已注册服务的诊断信息</para>
/// </summary>
public interface IModuleDiagnostics
{
    /// <summary>
    ///     <para xml:lang="en">Get all loaded modules in execution order</para>
    ///     <para xml:lang="zh">按执行顺序获取所有已加载的模块</para>
    /// </summary>
    IReadOnlyList<ModuleInfo> GetLoadedModules();

    /// <summary>
    ///     <para xml:lang="en">Get all auto-registered services</para>
    ///     <para xml:lang="zh">获取所有自动注册的服务</para>
    /// </summary>
    IReadOnlyList<ServiceRegistrationInfo> GetAutoRegisteredServices();

    /// <summary>
    ///     <para xml:lang="en">Validate module dependencies and check for issues</para>
    ///     <para xml:lang="zh">验证模块依赖并检查问题</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">List of validation issues, empty if no issues found</para>
    ///     <para xml:lang="zh">验证问题列表，如果没有问题则为空</para>
    /// </returns>
    IReadOnlyList<string> ValidateModuleDependencies();
}
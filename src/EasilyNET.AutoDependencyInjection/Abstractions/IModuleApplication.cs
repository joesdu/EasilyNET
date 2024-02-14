using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <inheritdoc />
public interface IModuleApplication : IDisposable
{
    /// <summary>
    /// 启动模块类型
    /// </summary>
    Type StartupModuleType { get; }

    /// <summary>
    /// IServiceCollection
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// IServiceProvider
    /// </summary>
    IServiceProvider? ServiceProvider { get; }

    /// <summary>
    /// Modules
    /// </summary>
    IReadOnlyList<IAppModule> Modules { get; }

    /// <summary>
    /// Source
    /// </summary>
    IEnumerable<IAppModule> Source { get; }
}

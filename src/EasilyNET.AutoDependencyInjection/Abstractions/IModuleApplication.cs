using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <inheritdoc />
internal interface IModuleApplication : IDisposable
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         <see cref="IServiceCollection" />
    ///     </para>
    ///     <para xml:lang="zh">
    ///         <see cref="IServiceCollection" />
    ///     </para>
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    ///     <para xml:lang="en">Modules ordered by dependency (dependencies first, startup module last)</para>
    ///     <para xml:lang="zh">按依赖顺序排列的模块（依赖项在前，启动模块在后）</para>
    /// </summary>
    IList<IAppModule> Modules { get; }
}
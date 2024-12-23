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
    ///     <para xml:lang="en">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    ///     <para xml:lang="zh">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    /// </summary>
    IServiceProvider? ServiceProvider { get; }

    /// <summary>
    ///     <para xml:lang="en">Modules</para>
    ///     <para xml:lang="zh">模块</para>
    /// </summary>
    IList<IAppModule> Modules { get; }
}
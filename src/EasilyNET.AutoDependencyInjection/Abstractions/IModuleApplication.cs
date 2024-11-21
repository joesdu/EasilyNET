using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <inheritdoc />
internal interface IModuleApplication : IDisposable
{
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
    IList<IAppModule> Modules { get; }
}
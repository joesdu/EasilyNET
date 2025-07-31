using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using EasilyNET.Ipc.Serializers;
using EasilyNET.Ipc.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Ipc;

/// <summary>
/// Provides extension methods for registering Inter-Process Communication (IPC) services and infrastructure into an
/// <see cref="IServiceCollection" />.
/// </summary>
/// <remarks>
/// These extension methods simplify the configuration and registration of IPC-related services,
/// including infrastructure, server, and client components. They allow for optional configuration via an
/// <see
///     cref="IConfiguration" />
/// instance and provide default settings for retry policies, timeouts, and
/// serialization.
/// </remarks>
public static class IpcServiceCollectionExtensions
{
    /// <summary>
    /// Adds the IPC (Inter-Process Communication) infrastructure services to the specified
    /// <see
    ///     cref="IServiceCollection" />
    /// .
    /// </summary>
    /// <remarks>
    /// This method registers the necessary services and configuration for IPC functionality,
    /// including retry policies, circuit breaker settings, timeouts, and serialization options. By default, the IPC
    /// infrastructure uses JSON serialization and predefined timeout and retry settings. If a configuration section
    /// matching the <see cref="IpcOptions.SectionName" /> is provided, it will be used to override the default
    /// settings.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the IPC services will be added.</param>
    /// <param name="configuration">
    /// An optional <see cref="IConfiguration" /> instance used to configure IPC options. If provided, the configuration
    /// values will override the default IPC settings.
    /// </param>
    /// <param name="configureOptions">
    /// An optional action to configure IPC options via code. If provided, it will be applied after configuration file settings.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection" /> with the IPC infrastructure services registered.</returns>
    private static IServiceCollection AddIpcInfrastructure(this IServiceCollection services, IConfiguration? configuration = null, Action<IpcOptions>? configureOptions = null)
    {
        services.Configure<IpcOptions>(options =>
        {
            options.RetryPolicy.MaxAttempts = 5;
            options.RetryPolicy.InitialDelay = TimeSpan.FromSeconds(1);
            options.CircuitBreaker.MinimumThroughput = 5;
            options.Timeout.Ipc = TimeSpan.FromSeconds(10);
            options.Timeout.Business = TimeSpan.FromSeconds(30);
            options.DefaultTimeout = TimeSpan.FromSeconds(30);
            options.MaxServerInstances = 4;
            options.ClientPipePoolSize = 2;
            options.Serializer = new JsonIpcSerializer();
        });
        if (configuration != null)
        {
            services.Configure<IpcOptions>(configuration.GetSection(IpcOptions.SectionName));
        }
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        services.AddSingleton<IIpcCommandService, IpcCommandService>();
        services.AddSingleton<IIpcSerializer, JsonIpcSerializer>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<IpcOptions>>().Value;
            return options.Serializer ?? new JsonIpcSerializer();
        });
        return services;
    }

    /// <summary>
    /// Adds the necessary services to enable an IPC (Inter-Process Communication) server.
    /// </summary>
    /// <remarks>
    /// This method registers the required infrastructure, command handler, and hosted service  to
    /// support IPC server functionality. It is typically used in the application's startup  configuration to enable
    /// IPC-based communication.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the IPC server services will be added.</param>
    /// <param name="configuration">
    /// An optional <see cref="IConfiguration" /> instance used to configure the IPC server.  If <see langword="null" />,
    /// default configuration settings will be applied.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection" /> instance, allowing for method chaining.</returns>
    public static IServiceCollection AddIpcServer(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddIpcInfrastructure(configuration);
        services.AddSingleton<IIpcCommandHandler, IpcCommandHandler>();
        services.AddHostedService<IpcCommandHandlerHostedService>();
        return services;
    }

    /// <summary>
    /// Adds the necessary services to enable an IPC (Inter-Process Communication) server with code-based configuration.
    /// </summary>
    /// <remarks>
    /// This method registers the required infrastructure, command handler, and hosted service  to
    /// support IPC server functionality. It allows configuration via code using the provided action.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the IPC server services will be added.</param>
    /// <param name="configureOptions">An action to configure the IPC options via code.</param>
    /// <returns>The same <see cref="IServiceCollection" /> instance, allowing for method chaining.</returns>
    public static IServiceCollection AddIpcServer(this IServiceCollection services, Action<IpcOptions> configureOptions)
    {
        services.AddIpcInfrastructure(configureOptions: configureOptions);
        services.AddSingleton<IIpcCommandHandler, IpcCommandHandler>();
        services.AddHostedService<IpcCommandHandlerHostedService>();
        return services;
    }

    /// <summary>
    /// Adds the necessary services to enable an IPC (Inter-Process Communication) server with both configuration file and code-based configuration.
    /// </summary>
    /// <remarks>
    /// This method registers the required infrastructure, command handler, and hosted service  to
    /// support IPC server functionality. Configuration from files will be applied first, then code-based configuration.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the IPC server services will be added.</param>
    /// <param name="configuration">
    /// An optional <see cref="IConfiguration" /> instance used to configure the IPC server.  If <see langword="null" />,
    /// default configuration settings will be applied.
    /// </param>
    /// <param name="configureOptions">An action to configure the IPC options via code.</param>
    /// <returns>The same <see cref="IServiceCollection" /> instance, allowing for method chaining.</returns>
    public static IServiceCollection AddIpcServer(this IServiceCollection services, IConfiguration? configuration, Action<IpcOptions> configureOptions)
    {
        services.AddIpcInfrastructure(configuration, configureOptions);
        services.AddSingleton<IIpcCommandHandler, IpcCommandHandler>();
        services.AddHostedService<IpcCommandHandlerHostedService>();
        return services;
    }

    /// <summary>
    /// Adds the IPC client services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <remarks>
    /// This method registers the necessary services for IPC client functionality, including the
    /// <see cref="IIpcClient" /> implementation. It also configures the IPC infrastructure using the  provided
    /// <paramref
    ///     name="configuration" />
    /// if supplied.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the IPC client services will be added.</param>
    /// <param name="configuration">
    /// An optional <see cref="IConfiguration" /> instance used to configure the IPC client.  If <c>null</c>, default
    /// configuration settings will be applied.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection" /> instance.</returns>
    public static IServiceCollection AddIpcClient(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddIpcInfrastructure(configuration);
        services.AddSingleton<IIpcClient, IpcClient>();
        return services;
    }

    /// <summary>
    /// Adds the IPC client services to the specified <see cref="IServiceCollection" /> with code-based configuration.
    /// </summary>
    /// <remarks>
    /// This method registers the necessary services for IPC client functionality, including the
    /// <see cref="IIpcClient" /> implementation. It allows configuration via code using the provided action.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the IPC client services will be added.</param>
    /// <param name="configureOptions">An action to configure the IPC options via code.</param>
    /// <returns>The updated <see cref="IServiceCollection" /> instance.</returns>
    public static IServiceCollection AddIpcClient(this IServiceCollection services, Action<IpcOptions> configureOptions)
    {
        services.AddIpcInfrastructure(configureOptions: configureOptions);
        services.AddSingleton<IIpcClient, IpcClient>();
        return services;
    }

    /// <summary>
    /// Adds the IPC client services to the specified <see cref="IServiceCollection" /> with both configuration file and code-based configuration.
    /// </summary>
    /// <remarks>
    /// This method registers the necessary services for IPC client functionality, including the
    /// <see cref="IIpcClient" /> implementation. Configuration from files will be applied first, then code-based configuration.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the IPC client services will be added.</param>
    /// <param name="configuration">
    /// An optional <see cref="IConfiguration" /> instance used to configure the IPC client.  If <c>null</c>, default
    /// configuration settings will be applied.
    /// </param>
    /// <param name="configureOptions">An action to configure the IPC options via code.</param>
    /// <returns>The updated <see cref="IServiceCollection" /> instance.</returns>
    public static IServiceCollection AddIpcClient(this IServiceCollection services, IConfiguration? configuration, Action<IpcOptions> configureOptions)
    {
        services.AddIpcInfrastructure(configuration, configureOptions);
        services.AddSingleton<IIpcClient, IpcClient>();
        return services;
    }
}
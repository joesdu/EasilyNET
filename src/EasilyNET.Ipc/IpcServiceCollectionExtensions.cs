using System.Reflection;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using EasilyNET.Ipc.Serializers;
using EasilyNET.Ipc.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Ipc;

/// <summary>
/// IPC 服务注册扩展，提供统一的 IPC 服务端和客户端配置方式
/// </summary>
/// <remarks>
/// 此扩展类是 IPC 库的统一入口点，提供了简化的 API 来配置和注册 IPC 相关服务。
/// 支持服务端、客户端的注册，以及命令和处理器的注册。
/// 主要功能：
/// - 注册 IPC 服务端（AddIpcServer）
/// - 注册 IPC 客户端（AddIpcClient）
/// - 注册命令处理器（AddIpcCommandHandler）
/// - 批量注册程序集中的所有命令（RegisterIpcCommandsFromAssembly）
/// </remarks>
public static class IpcServiceCollectionExtensions
{
    #region 核心基础设施注册

    /// <summary>
    /// 添加 IPC 基础设施服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置实例（可选）</param>
    /// <param name="configureOptions">代码配置操作（可选）</param>
    /// <returns>服务集合，支持链式调用</returns>
    private static IServiceCollection AddIpcInfrastructure(this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<IpcOptions>? configureOptions = null)
    {
        // 配置默认选项
        services.Configure<IpcOptions>(options =>
        {
            // 重试策略配置
            options.RetryPolicy.MaxAttempts = 5;
            options.RetryPolicy.InitialDelay = TimeSpan.FromSeconds(1);
            options.RetryPolicy.BackoffType = "Exponential";
            options.RetryPolicy.UseJitter = true;

            // 熔断器配置
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

            // 超时配置
            options.Timeout.Ipc = TimeSpan.FromSeconds(10);
            options.Timeout.Business = TimeSpan.FromSeconds(30);
            options.DefaultTimeout = TimeSpan.FromSeconds(30);

            // 服务配置
            options.PipeName = "EasilyNET_IPC";
            options.TransportCount = 4;
            options.MaxServerInstances = 4;
            options.ClientPipePoolSize = 2;
        });

        // 应用配置文件配置
        if (configuration != null)
        {
            services.Configure<IpcOptions>(configuration.GetSection(IpcOptions.SectionName));
        }

        // 应用代码配置
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // 注册核心服务 - 统一使用 IpcCommandRegistry
        services.AddSingleton<IIpcCommandRegistry, IpcCommandRegistry>();
        services.AddSingleton<IIpcCommandService, IpcCommandService>();
        services.AddSingleton<IIpcGenericSerializer, AdvancedJsonIpcSerializer>();
        return services;
    }

    #endregion

    #region 内部帮助类

    /// <summary>
    /// 命令注册服务，用于在应用启动时注册命令
    /// </summary>
    private sealed class CommandRegistrationService(Action registrationAction) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            registrationAction();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    #endregion

    #region 服务端注册

    /// <summary>
    /// 添加 IPC 服务端
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置实例（可选）</param>
    /// <returns>服务集合，支持链式调用</returns>
    /// <example>
    ///     <code>
    /// // 基础注册
    /// services.AddIpcServer();
    /// 
    /// // 使用配置文件
    /// services.AddIpcServer(configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddIpcServer(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddIpcInfrastructure(configuration);
        services.AddSingleton<IpcCommandHandler>();
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
        services.AddSingleton<IpcCommandHandler>();
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
        services.AddSingleton<IpcCommandHandler>();
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

    #endregion

    #region 命令和处理器注册

    /// <summary>
    /// 注册 IPC 命令处理器
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <typeparam name="TPayload">负载数据类型</typeparam>
    /// <typeparam name="TResponse">响应数据类型</typeparam>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="commandTypeName">命令类型名称（可选，默认使用类名）</param>
    /// <param name="handlerLifetime">处理器服务生命周期</param>
    /// <returns>服务集合，支持链式调用</returns>
    /// <example>
    ///     <code>
    /// // 注册命令处理器
    /// services.AddIpcCommandHandler&lt;MyCommand, MyPayload, MyResponse, MyCommandHandler&gt;();
    /// 
    /// // 指定命令名称和生命周期
    /// services.AddIpcCommandHandler&lt;MyCommand, MyPayload, MyResponse, MyCommandHandler&gt;(
    ///     commandTypeName: "CustomCommandName",
    ///     handlerLifetime: ServiceLifetime.Singleton
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection AddIpcCommandHandler<TCommand, TPayload, TResponse, THandler>(
        this IServiceCollection services,
        string? commandTypeName = null,
        ServiceLifetime handlerLifetime = ServiceLifetime.Scoped)
        where TCommand : class, IIpcCommand<TPayload>
        where THandler : class, IIpcCommandHandler<TCommand, TPayload, TResponse>
    {
        // 注册处理器
        services.Add(new(typeof(THandler), typeof(THandler), handlerLifetime));
        services.Add(new(typeof(IIpcCommandHandler<TCommand, TPayload, TResponse>), typeof(THandler), handlerLifetime));

        // 注册命令初始化回调
        services.AddSingleton<IHostedService>(provider => new CommandRegistrationService(() =>
        {
            var commandRegistry = provider.GetRequiredService<IpcCommandRegistry>();

            // 注册到 IpcCommandRegistry
            commandRegistry.RegisterCommand<TCommand, TPayload>(commandTypeName, typeof(TResponse));

            // 注册到统一的命令注册表
            commandRegistry.Register<TCommand>();
        }));
        return services;
    }

    /// <summary>
    /// 批量注册程序集中的所有 IPC 命令类型
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>服务集合，支持链式调用</returns>
    /// <example>
    ///     <code>
    /// // 注册当前程序集的所有命令
    /// services.RegisterIpcCommandsFromAssembly(Assembly.GetExecutingAssembly());
    /// 
    /// // 注册多个程序集的所有命令
    /// services.RegisterIpcCommandsFromAssembly(
    ///     Assembly.GetExecutingAssembly(),
    ///     typeof(SomeCommand).Assembly
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection RegisterIpcCommandsFromAssembly(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<IHostedService>(provider => new CommandRegistrationService(() =>
        {
            var typeRegistry = provider.GetRequiredService<IIpcCommandRegistry>();
            foreach (var assembly in assemblies)
            {
                typeRegistry.RegisterFromAssembly(assembly);
            }
        }));
        return services;
    }

    /// <summary>
    /// 手动注册单个 IPC 命令类型
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    /// <example>
    ///     <code>
    /// services.RegisterIpcCommand&lt;MyCommand&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterIpcCommand<TCommand>(this IServiceCollection services)
        where TCommand : class, IIpcCommandBase
    {
        services.AddSingleton<IHostedService>(provider => new CommandRegistrationService(() =>
        {
            var typeRegistry = provider.GetRequiredService<IIpcCommandRegistry>();
            typeRegistry.Register<TCommand>();
        }));
        return services;
    }

    #endregion
}
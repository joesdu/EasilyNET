using EasilyNET.Ipc.Abstractions;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Serializers;
using EasilyNET.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.Ipc.Extensions;

/// <summary>
/// IPC 高级服务注册扩展
/// </summary>
public static class IpcAdvancedServiceCollectionExtensions
{
    /// <summary>
    /// 添加高级 IPC 服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAdvancedIpc(this IServiceCollection services)
    {
        // 注册核心服务
        services.AddSingleton<IpcCommandRegistry>();
        services.AddSingleton<IpcCommandDispatcher>();
        services.AddSingleton<IIpcGenericSerializer, AdvancedJsonIpcSerializer>();
        return services;
    }

    /// <summary>
    /// 注册 IPC 命令和处理器
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <typeparam name="TPayload">负载数据类型</typeparam>
    /// <typeparam name="TResponse">响应数据类型</typeparam>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="commandTypeName">命令类型名称（可选，默认使用类名）</param>
    /// <param name="version">版本号</param>
    /// <param name="handlerLifetime">处理器服务生命周期</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddIpcCommandHandler<TCommand, TPayload, TResponse, THandler>(
        this IServiceCollection services,
        string? commandTypeName = null,
        int version = 1,
        ServiceLifetime handlerLifetime = ServiceLifetime.Scoped)
        where TCommand : class, IIpcCommand<TPayload>
        where THandler : class, IIpcCommandHandler<TCommand, TPayload, TResponse>
    {
        // 注册处理器
        services.Add(new(typeof(THandler), typeof(THandler), handlerLifetime));

        // 注册命令类型和处理器映射
        services.Configure<IpcConfiguration>(config =>
        {
            var typeName = commandTypeName ?? typeof(TCommand).Name;
            config.CommandRegistrations.Add(new()
            {
                CommandType = typeof(TCommand),
                PayloadType = typeof(TPayload),
                ResponseType = typeof(TResponse),
                HandlerType = typeof(THandler),
                CommandTypeName = typeName,
                Version = version
            });
        });
        return services;
    }
}

/// <summary>
/// IPC 配置
/// </summary>
public class IpcConfiguration
{
    /// <summary>
    /// 命令注册列表
    /// </summary>
    public List<CommandRegistration> CommandRegistrations { get; } = new();
}

/// <summary>
/// 命令注册信息
/// </summary>
public class CommandRegistration
{
    /// <summary>
    /// 命令类型
    /// </summary>
    public Type CommandType { get; set; } = null!;

    /// <summary>
    /// 负载数据类型
    /// </summary>
    public Type PayloadType { get; set; } = null!;

    /// <summary>
    /// 响应数据类型
    /// </summary>
    public Type ResponseType { get; set; } = null!;

    /// <summary>
    /// 处理器类型
    /// </summary>
    public Type HandlerType { get; set; } = null!;

    /// <summary>
    /// 命令类型名称
    /// </summary>
    public string CommandTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public int Version { get; set; } = 1;
}
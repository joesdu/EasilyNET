using System.Collections.Concurrent;
using EasilyNET.Ipc.Abstractions;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// IPC 命令分发器
/// </summary>
public class IpcCommandDispatcher
{
    private readonly ConcurrentDictionary<string, Type> _handlerTypes = new();
    private readonly ILogger<IpcCommandDispatcher> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化新的命令分发器实例
    /// </summary>
    /// <param name="serviceProvider">服务提供程序</param>
    /// <param name="logger">日志记录器</param>
    public IpcCommandDispatcher(IServiceProvider serviceProvider, ILogger<IpcCommandDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 注册命令处理器
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <typeparam name="TPayload">负载数据类型</typeparam>
    /// <typeparam name="TResponse">响应数据类型</typeparam>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="commandTypeName">命令类型名称</param>
    public void RegisterHandler<TCommand, TPayload, TResponse, THandler>(string commandTypeName)
        where TCommand : class, IIpcCommand<TPayload>
        where THandler : class, IIpcCommandHandler<TCommand, TPayload, TResponse>
    {
        _handlerTypes.AddOrUpdate(commandTypeName, typeof(THandler), (_, _) => typeof(THandler));
        _logger.LogDebug("已注册命令处理器: {CommandType} -> {HandlerType}", commandTypeName, typeof(THandler).Name);
    }

    /// <summary>
    /// 分发命令到对应的处理器
    /// </summary>
    /// <param name="command">要分发的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    public async Task<IpcCommandResponse> DispatchAsync(IIpcCommandBase command, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取命令类型名称
            var commandTypeName = GetCommandTypeName(command);
            if (commandTypeName == null)
            {
                var errorMsg = $"无法确定命令类型: {command.GetType().Name}";
                _logger.LogError(errorMsg);
                return CreateErrorResponse(command.CommandId, errorMsg);
            }

            // 查找处理器类型
            if (!_handlerTypes.TryGetValue(commandTypeName, out var handlerType))
            {
                var errorMsg = $"未找到命令处理器: {commandTypeName}";
                _logger.LogWarning(errorMsg);
                return CreateErrorResponse(command.CommandId, errorMsg);
            }

            // 获取处理器实例
            var handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
            {
                var errorMsg = $"无法创建处理器实例: {handlerType.Name}";
                _logger.LogError(errorMsg);
                return CreateErrorResponse(command.CommandId, errorMsg);
            }

            // 使用反射调用处理器的 HandleAsync 方法
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
            {
                var errorMsg = $"处理器 {handlerType.Name} 没有 HandleAsync 方法";
                _logger.LogError(errorMsg);
                return CreateErrorResponse(command.CommandId, errorMsg);
            }
            var result = handleMethod.Invoke(handler, [command, cancellationToken]);
            if (result is Task task)
            {
                await task;
                var property = task.GetType().GetProperty("Result");
                if (property?.GetValue(task) is IpcCommandResponse response)
                {
                    return response;
                }
            }
            var unknownErrorMsg = "处理器返回了未知的结果类型";
            _logger.LogError(unknownErrorMsg);
            return CreateErrorResponse(command.CommandId, unknownErrorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"处理命令时发生异常: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            return CreateErrorResponse(command.CommandId, errorMsg);
        }
    }

    /// <summary>
    /// 获取命令类型名称
    /// </summary>
    /// <param name="command">命令实例</param>
    /// <returns>命令类型名称</returns>
    private static string GetCommandTypeName(IIpcCommandBase command)
    {
        // 如果命令实现了额外的接口来提供类型名称
        if (command is IHasCommandTypeName namedCommand)
        {
            return namedCommand.CommandTypeName;
        }

        // 否则使用类型名称
        return command.GetType().Name;
    }

    /// <summary>
    /// 创建错误响应
    /// </summary>
    /// <param name="commandId">命令 ID</param>
    /// <param name="errorMessage">错误消息</param>
    /// <returns>错误响应</returns>
    private static IpcCommandResponse CreateErrorResponse(string commandId, string errorMessage) =>
        new()
        {
            CommandId = commandId,
            Success = false,
            Message = errorMessage
        };
}

/// <summary>
/// 具有命令类型名称的接口
/// </summary>
public interface IHasCommandTypeName
{
    /// <summary>
    /// 命令类型名称
    /// </summary>
    string CommandTypeName { get; }
}
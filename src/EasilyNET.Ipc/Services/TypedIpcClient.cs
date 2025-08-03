using System.Text.Json;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// 强类型IPC客户端实现
/// </summary>
public sealed class TypedIpcClient : IDisposable
{
    private readonly IIpcClient _ipcClient;
    private readonly ILogger<TypedIpcClient>? _logger;
    private readonly CommandTypeRegistry _typeRegistry;

    /// <summary>
    /// 初始化类型化IPC客户端的新实例
    /// </summary>
    /// <param name="ipcClient">底层IPC客户端实例</param>
    /// <param name="typeRegistry">命令类型注册表，用于类型解析和哈希生成</param>
    /// <param name="logger">可选的日志记录器</param>
    public TypedIpcClient(IIpcClient ipcClient, CommandTypeRegistry typeRegistry, ILogger<TypedIpcClient>? logger = null)
    {
        _ipcClient = ipcClient;
        _typeRegistry = typeRegistry;
        _logger = logger;
    }

    /// <summary>
    /// 释放资源，包括底层IPC客户端
    /// </summary>
    public void Dispose()
    {
        _ipcClient?.Dispose();
    }

    /// <summary>
    /// 发送带响应的强类型命令
    /// </summary>
    public async Task<TResponse> SendAsync<TResponse>(ITypedCommand<TResponse> command, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        try
        {
            var commandType = command.GetType();
            var typeHash = _typeRegistry.GetTypeHash(commandType);
            _logger?.LogDebug("发送强类型命令: {CommandType} (Hash: {TypeHash})", commandType.Name, typeHash);

            // 创建强类型消息
            var message = new TypedIpcMessage
            {
                TypeHash = typeHash,
                RequiresResponse = true,
                PayloadData = JsonSerializer.SerializeToUtf8Bytes(command),
                TargetId = null
            };

            // 序列化消息为传统IPC格式
            var ipcCommand = new IpcCommand
            {
                CommandType = "TypedCommand", // 固定标识符
                PayloadBytes = MessagePackSerializer.Serialize(message)
            };

            // 发送命令
            var response = await _ipcClient.SendCommandAsync(ipcCommand, timeout);
            if (response == null)
            {
                throw new InvalidOperationException("未收到IPC响应");
            }
            if (!response.Success)
            {
                throw new InvalidOperationException($"IPC命令执行失败: {response.Message}");
            }

            // 反序列化强类型响应
            var typedResponse = MessagePackSerializer.Deserialize<TypedIpcResponse>(response.DataBytes.ToArray());
            if (!typedResponse.Success)
            {
                throw new InvalidOperationException($"命令执行失败: {typedResponse.ErrorMessage}");
            }
            return JsonSerializer.Deserialize<TResponse>(typedResponse.ResponseData.Span)!;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "发送强类型命令时发生错误: {CommandType}", command.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// 发送无响应的强类型命令
    /// </summary>
    public async Task SendAsync(ITypedCommand command, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        try
        {
            var commandType = command.GetType();
            var typeHash = _typeRegistry.GetTypeHash(commandType);
            _logger?.LogDebug("发送强类型命令（无响应）: {CommandType} (Hash: {TypeHash})", commandType.Name, typeHash);
            var message = new TypedIpcMessage
            {
                TypeHash = typeHash,
                RequiresResponse = false,
                PayloadData = JsonSerializer.SerializeToUtf8Bytes(command)
            };
            var ipcCommand = new IpcCommand
            {
                CommandType = "TypedCommand",
                PayloadBytes = MessagePackSerializer.Serialize(message)
            };
            var response = await _ipcClient.SendCommandAsync(ipcCommand, timeout);
            if (response != null && !response.Success)
            {
                throw new InvalidOperationException($"IPC命令执行失败: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "发送强类型命令时发生错误: {CommandType}", command.GetType().Name);
            throw;
        }
    }
}
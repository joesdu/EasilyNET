using System.Collections.Concurrent;
using System.Text.Json;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using EasilyNET.Ipc.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// Handles inter-process communication (IPC) commands by managing transports, processing commands, and providing
/// responses.
/// </summary>
/// <remarks>
/// This class is responsible for starting and stopping IPC communication, managing multiple transport
/// instances, and processing commands received from connected clients. It supports retry policies and timeout
/// configurations for robust communication handling. The class is designed to be used in scenarios where reliable IPC
/// is required, such as between different processes or services on the same machine. The
/// <see cref="IpcCommandHandler" /> implements <see cref="IIpcLifetime" />, and
/// <see cref="IDisposable" /> to provide a complete lifecycle management for IPC operations.
/// </remarks>
public sealed class IpcCommandHandler : IIpcLifetime, IDisposable
{
    private readonly ILogger _logger;
    private readonly IpcOptions _options;
    private readonly ConcurrentDictionary<string, DateTime> _processedCommands = new();
    private readonly IIpcGenericSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, IIpcTransport> _transports = new();
    private bool _disposed;
    private bool _isStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcCommandHandler" /> class with the specified service provider,
    /// options, logger, and serializer.
    /// </summary>
    /// <remarks>
    /// This constructor sets up the IPC command handler with all necessary dependencies for processing
    /// commands. The service provider is used to resolve command handlers, the options configure timeouts and retry
    /// policies, the logger provides diagnostic information, and the serializer handles command serialization and
    /// deserialization.
    /// </remarks>
    /// <param name="serviceProvider">
    /// The service provider for resolving dependencies, including command handlers. Cannot be
    /// <see langword="null" />.
    /// </param>
    /// <param name="options">
    /// The IPC options for configuring timeouts, retry policies, and transport settings. Cannot be
    /// <see langword="null" />.
    /// </param>
    /// <param name="logger">The logger for diagnostic and operational information. Cannot be <see langword="null" />.</param>
    /// <param name="serializer">
    /// The serializer for converting commands to and from byte arrays. Cannot be <see langword="null" />.
    /// </param>
    public IpcCommandHandler(IServiceProvider serviceProvider, IOptions<IpcOptions> options, ILogger<IpcCommandHandler> logger, IIpcGenericSerializer serializer)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger.LogInformation("IPC 命令处理器已初始化");
    }

    private IpcCommandRegistry CommandRegistry => _serviceProvider.GetRequiredService<IpcCommandRegistry>();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        try
        {
            if (_isStarted)
            {
                Stop();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止 IPC 命令处理器时发生错误");
        }
        foreach (var transport in _transports.Values)
        {
            transport.Dispose();
        }
        _transports.Clear();
        _disposed = true;
    }

    /// <inheritdoc />
    public Task StartAsync()
    {
        Start();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        Stop();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理单个IPC命令
    /// </summary>
    public async Task<IpcCommandResponse<object>> HandleCommandAsync(IIpcCommand<object> command)
    {
        using var scope = _serviceProvider.CreateScope();
        try
        {
            // 这里应该基于命令类型路由到正确的处理器
            // 目前返回一个基本的成功响应
            _logger.LogDebug("处理 IPC 命令: CommandId: {CommandId}", command.CommandId);

            // 模拟处理逻辑
            await Task.Delay(10); // 模拟异步处理
            return IpcCommandResponse<object>.CreateSuccess(command.CommandId, "Command processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理命令时发生错误: CommandId: {CommandId}", command.CommandId);
            return IpcCommandResponse<object>.CreateFailure(command.CommandId, ex.Message);
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        if (_isStarted)
        {
            return;
        }
        try
        {
            var transportCount = _options.TransportCount;
            for (var i = 0; i < transportCount; i++)
            {
                var transportId = $"transport_{i}";
                var transport = CreateTransport(transportId);
                _transports[transportId] = transport;
                transport.Start();
                _logger.LogInformation("IPC 传输 {TransportId} 已启动", transportId);
            }
            _isStarted = true;
            _logger.LogInformation("IPC 命令处理器已启动，共 {TransportCount} 个传输", transportCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动 IPC 命令处理器时发生错误");
            throw;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (!_isStarted)
        {
            return;
        }
        try
        {
            foreach (var transport in _transports.Values)
            {
                transport.Stop();
            }
            _isStarted = false;
            _logger.LogInformation("IPC 命令处理器已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止 IPC 命令处理器时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 创建传输实例
    /// </summary>
    private IIpcTransport CreateTransport(string transportId)
    {
        var pipeName = $"{_options.PipeName}_{transportId}";
        IIpcTransport transport = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => new NamedPipeTransport(pipeName, true, _logger),
            PlatformID.Unix    => new UnixSocketTransport($"{_options.UnixSocketPath}_{transportId}", true, _logger),
            _                  => throw new PlatformNotSupportedException($"不支持的操作系统平台: {Environment.OSVersion.Platform}")
        };

        // 设置命令处理事件
        transport.CommandReceived += async (sender, commandData) =>
        {
            IpcResponse? response = null;
            IpcMessage<byte[]>? message = null;
            try
            {
                // 1. 反序列化外层消息
                message = _serializer.Deserialize<IpcMessage<byte[]>>(commandData);
                if (message == null)
                {
                    _logger.LogWarning("无法反序列化IPC消息。");
                    response = IpcResponse.CreateError("无效的消息格式");
                    return;
                }

                // 2. 查找命令类型
                var commandMetadata = CommandRegistry.GetMetadata(message.TypeHash);
                if (commandMetadata == null)
                {
                    _logger.LogWarning("未找到类型哈希为 {TypeHash} 的命令", message.TypeHash);
                    response = IpcResponse.CreateError($"未知的命令类型哈希: {message.TypeHash}", message.MessageId);
                    return;
                }

                // 3. 反序列化内部命令
                var command = (IIpcCommandBase?)JsonSerializer.Deserialize(message.Payload, commandMetadata.CommandType);
                if (command == null)
                {
                    _logger.LogWarning("无法反序列化命令负载: {CommandType}", commandMetadata.CommandType.Name);
                    response = IpcResponse.CreateError("无效的命令负载", message.MessageId);
                    return;
                }
                _logger.LogDebug("接收到 IPC 命令: {CommandType} (ID: {CommandId})", commandMetadata.CommandType.Name, command.CommandId);

                // 4. 处理命令
                using var scope = _serviceProvider.CreateScope();
                if (commandMetadata.PayloadType is null || commandMetadata.ResponseType is null)
                {
                    _logger.LogError("命令元数据不完整，缺少 PayloadType 或 ResponseType: {CommandType}", commandMetadata.CommandType.Name);
                    response = IpcResponse.CreateError($"命令 '{commandMetadata.CommandType.Name}' 的元数据不完整", message.MessageId);
                    return;
                }
                var handlerType = typeof(IIpcCommandHandler<,,>).MakeGenericType(commandMetadata.CommandType, commandMetadata.PayloadType, commandMetadata.ResponseType);
                var handler = scope.ServiceProvider.GetService(handlerType);
                if (handler == null)
                {
                    _logger.LogError("未找到命令处理器: {CommandType}", commandMetadata.CommandType.Name);
                    response = IpcResponse.CreateError($"未找到命令 '{commandMetadata.CommandType.Name}' 的处理器", message.MessageId);
                    return;
                }
                var handleMethod = handler.GetType().GetMethod("HandleAsync");
                if (handleMethod == null)
                {
                    _logger.LogError("在处理器 {HandlerType} 中未找到 HandleAsync 方法", handler.GetType().Name);
                    response = IpcResponse.CreateError($"处理器 '{handler.GetType().Name}' 中缺少 HandleAsync 方法", message.MessageId);
                    return;
                }
                var resultTask = (Task)handleMethod.Invoke(handler, [command, CancellationToken.None])!;
                await resultTask;
                var resultProperty = resultTask.GetType().GetProperty("Result");
                var result = resultProperty?.GetValue(resultTask);

                // 5. 创建并序列化响应
                response = IpcResponse.CreateSuccess(result, message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理接收到的命令时发生错误");
                response = IpcResponse.CreateError($"处理命令时发生内部错误: {ex.Message}", message?.MessageId);
            }
            finally
            {
                // 6. 发送响应 (如果需要)
                if (response != null && sender is IIpcTransport senderTransport)
                {
                    var responseData = _serializer.Serialize(response);
                    await senderTransport.SendResponseAsync(responseData);
                }
            }
        };
        return transport;
    }
}
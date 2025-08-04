using EasilyNET.Core.Essentials;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Server.Sample.Commands;

#region 计算命令

/// <summary>
/// 数学计算负载
/// </summary>
public class MathCalculationPayload
{
    public double Number1 { get; set; }
    public double Number2 { get; set; }
    public string Operation { get; set; } = string.Empty; // +, -, *, /
}

/// <summary>
/// 数学计算结果
/// </summary>
public class MathResult
{
    public double Result { get; set; }
    public string Operation { get; set; } = string.Empty;
    public double Number1 { get; set; }
    public double Number2 { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 数学计算命令
/// </summary>
public class MathCalculationCommand : IIpcCommand<MathCalculationPayload>
{
    public MathCalculationPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 数学计算处理器
/// </summary>
public class MathCalculationHandler : IIpcCommandHandler<MathCalculationCommand, MathCalculationPayload, MathResult>
{
    private readonly ILogger<MathCalculationHandler> _logger;

    public MathCalculationHandler(ILogger<MathCalculationHandler> logger)
    {
        _logger = logger;
    }

    public Task<IpcCommandResponse<MathResult>> HandleAsync(MathCalculationCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("执行数学运算: {Number1} {Operation} {Number2}",
                command.Payload.Number1, command.Payload.Operation, command.Payload.Number2);

            double result = command.Payload.Operation switch
            {
                "+" => command.Payload.Number1 + command.Payload.Number2,
                "-" => command.Payload.Number1 - command.Payload.Number2,
                "*" => command.Payload.Number1 * command.Payload.Number2,
                "/" when command.Payload.Number2 != 0 => command.Payload.Number1 / command.Payload.Number2,
                "/" => throw new DivideByZeroException("除数不能为零"),
                _ => throw new ArgumentException($"不支持的运算符: {command.Payload.Operation}")
            };

            var mathResult = new MathResult
            {
                Result = result,
                Operation = command.Payload.Operation,
                Number1 = command.Payload.Number1,
                Number2 = command.Payload.Number2
            };

            _logger.LogInformation("运算完成，结果: {Result}", result);
            return Task.FromResult(IpcCommandResponse<MathResult>.CreateSuccess(command.CommandId, mathResult, "计算成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数学运算失败");
            return Task.FromResult(IpcCommandResponse<MathResult>.CreateFailure(command.CommandId, $"计算失败: {ex.Message}"));
        }
    }
}

#endregion

#region 延时命令

/// <summary>
/// 延时处理负载
/// </summary>
public class DelayProcessPayload
{
    public int DelaySeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 延时处理结果
/// </summary>
public class DelayResult
{
    public string Message { get; set; } = string.Empty;
    public int DelaySeconds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double ActualDelayMs { get; set; }
}

/// <summary>
/// 延时处理命令
/// </summary>
public class DelayProcessCommand : IIpcCommand<DelayProcessPayload>
{
    public DelayProcessPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 延时处理器（用于测试长时间运行的命令）
/// </summary>
public class DelayProcessHandler : IIpcCommandHandler<DelayProcessCommand, DelayProcessPayload, DelayResult>
{
    private readonly ILogger<DelayProcessHandler> _logger;

    public DelayProcessHandler(ILogger<DelayProcessHandler> logger)
    {
        _logger = logger;
    }

    public async Task<IpcCommandResponse<DelayResult>> HandleAsync(DelayProcessCommand command, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            _logger.LogInformation("开始延时处理: {DelaySeconds}秒, 消息: {Message}",
                command.Payload.DelaySeconds, command.Payload.Message);

            // 模拟长时间处理
            await Task.Delay(TimeSpan.FromSeconds(command.Payload.DelaySeconds), cancellationToken);

            var endTime = DateTime.UtcNow;
            var actualDelay = (endTime - startTime).TotalMilliseconds;

            var result = new DelayResult
            {
                Message = $"延时处理完成: {command.Payload.Message}",
                DelaySeconds = command.Payload.DelaySeconds,
                StartTime = startTime,
                EndTime = endTime,
                ActualDelayMs = actualDelay
            };

            _logger.LogInformation("延时处理完成，实际延时: {ActualDelayMs}ms", actualDelay);
            return IpcCommandResponse<DelayResult>.CreateSuccess(command.CommandId, result, "延时处理成功");
        }
        catch (OperationCanceledException)
        {
            var endTime = DateTime.UtcNow;
            var actualDelay = (endTime - startTime).TotalMilliseconds;
            _logger.LogWarning("延时处理被取消，已运行: {ActualDelayMs}ms", actualDelay);
            return IpcCommandResponse<DelayResult>.CreateFailure(command.CommandId, "处理被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延时处理失败");
            return IpcCommandResponse<DelayResult>.CreateFailure(command.CommandId, $"延时处理失败: {ex.Message}");
        }
    }
}

#endregion

#region 错误测试命令

/// <summary>
/// 错误测试负载
/// </summary>
public class ErrorTestPayload
{
    public string ErrorType { get; set; } = string.Empty; // ArgumentException, InvalidOperation, Custom
    public string? CustomMessage { get; set; }
}

/// <summary>
/// 错误测试命令
/// </summary>
public class ErrorTestCommand : IIpcCommand<ErrorTestPayload>
{
    public ErrorTestPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 错误测试处理器（用于测试错误处理机制）
/// </summary>
public class ErrorTestHandler : IIpcCommandHandler<ErrorTestCommand, ErrorTestPayload, string>
{
    private readonly ILogger<ErrorTestHandler> _logger;

    public ErrorTestHandler(ILogger<ErrorTestHandler> logger)
    {
        _logger = logger;
    }

    public Task<IpcCommandResponse<string>> HandleAsync(ErrorTestCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("模拟错误类型: {ErrorType}", command.Payload.ErrorType);

            // 根据错误类型抛出不同的异常
            switch (command.Payload.ErrorType.ToLowerInvariant())
            {
                case "argumentexception":
                    throw new ArgumentException("这是一个参数异常测试");

                case "invalidoperation":
                    throw new InvalidOperationException("这是一个无效操作异常测试");

                case "timeout":
                    throw new TimeoutException("这是一个超时异常测试");

                case "custom":
                    throw new InvalidDataException(command.Payload.CustomMessage ?? "这是一个自定义异常测试");

                default:
                    return Task.FromResult(IpcCommandResponse<string>.CreateSuccess(
                        command.CommandId,
                        "没有错误",
                        "未知的错误类型，正常处理"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按预期抛出异常: {ErrorType}", command.Payload.ErrorType);
            return Task.FromResult(IpcCommandResponse<string>.CreateFailure(command.CommandId, ex.Message));
        }
    }
}

#endregion

#region 状态查询命令

/// <summary>
/// 服务器状态
/// </summary>
public class ServerStatus
{
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }
    public int ProcessedCommands { get; set; }
    public int ActiveConnections { get; set; }
    public string ServerVersion { get; set; } = string.Empty;
    public Dictionary<string, object> Statistics { get; set; } = new();
}

/// <summary>
/// 获取服务器状态命令
/// </summary>
public class GetServerStatusCommand : IIpcCommand<object?>
{
    public object? Payload { get; set; }
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 服务器状态处理器
/// </summary>
public class GetServerStatusHandler : IIpcCommandHandler<GetServerStatusCommand, object?, ServerStatus>
{
    private readonly ILogger<GetServerStatusHandler> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private static int _processedCommands = 0;

    public GetServerStatusHandler(ILogger<GetServerStatusHandler> logger)
    {
        _logger = logger;
    }

    public Task<IpcCommandResponse<ServerStatus>> HandleAsync(GetServerStatusCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            Interlocked.Increment(ref _processedCommands);

            var status = new ServerStatus
            {
                StartTime = _startTime,
                Uptime = DateTime.UtcNow - _startTime,
                ProcessedCommands = _processedCommands,
                ActiveConnections = 1, // 简化处理
                ServerVersion = "1.0.0",
                Statistics = new Dictionary<string, object>
                {
                    { "MemoryUsage", GC.GetTotalMemory(false) },
                    { "ThreadCount", Environment.ProcessorCount },
                    { "MachineName", Environment.MachineName },
                    { "OSVersion", Environment.OSVersion.ToString() }
                }
            };

            _logger.LogInformation("返回服务器状态，运行时间: {Uptime}, 处理命令数: {ProcessedCommands}",
                status.Uptime, status.ProcessedCommands);

            return Task.FromResult(IpcCommandResponse<ServerStatus>.CreateSuccess(command.CommandId, status, "获取服务器状态成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取服务器状态失败");
            return Task.FromResult(IpcCommandResponse<ServerStatus>.CreateFailure(command.CommandId, $"获取服务器状态失败: {ex.Message}"));
        }
    }
}

#endregion

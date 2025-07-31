using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// Represents a client for inter-process communication (IPC) that sends commands and receives responses with built-in
/// resilience mechanisms such as retries and circuit breakers.
/// </summary>
/// <remarks>
/// This class provides a robust mechanism for sending IPC commands with configurable retry policies,
/// circuit breaker functionality, and timeout handling. It is designed to handle transient failures and ensure reliable
/// communication between processes. The client is initialized with options that define the behavior of these resilience
/// mechanisms.
/// </remarks>
public sealed class IpcClient : IIpcClient
{
    private readonly ResiliencePipeline _circuitBreakerPipeline;
    private readonly IIpcCommandService _commandService;
    private readonly ILogger? _logger;
    private readonly IpcOptions _options;
    private readonly ResiliencePipeline _retryPipeline;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcClient" /> class, which provides inter-process communication
    /// (IPC) functionality with built-in resilience mechanisms such as retries, timeouts, and circuit breakers.
    /// </summary>
    /// <remarks>
    /// The <see cref="IpcClient" /> is designed to handle transient failures and ensure robust IPC
    /// operations by leveraging resilience policies:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         Retry policy: Handles
    ///         specific exceptions such as <see cref="InvalidOperationException" />, <see cref="IOException" />,
    ///         <see
    ///             cref="TimeoutException" />
    ///         , and <see cref="TaskCanceledException" /> with configurable retry attempts, delays, and
    ///         backoff strategies.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Timeout policy: Enforces a timeout for IPC
    ///         operations based on the configured business timeout value.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Circuit
    ///         breaker policy: Monitors failure rates and temporarily halts operations when a failure threshold is exceeded,
    ///         with automatic recovery after a specified duration.
    ///         </description>
    ///     </item>
    /// </list>
    /// Logging is used to provide
    /// insights into retry attempts, circuit breaker state changes, and initialization status.
    /// </remarks>
    /// <param name="commandService">The service used to execute IPC commands. This parameter cannot be null.</param>
    /// <param name="options">
    /// The configuration options for the IPC client, including retry policies, timeouts, and circuit breaker settings.
    /// This parameter cannot be null.
    /// </param>
    /// <param name="logger">An optional logger instance for logging diagnostic and operational information. If null, no logging will occur.</param>
    public IpcClient(IIpcCommandService commandService, IOptions<IpcOptions> options, ILogger<IpcClient>? logger = null)
    {
        _commandService = commandService;
        _logger = logger;
        _options = options.Value;
        var retryOptions = _options.RetryPolicy;
        _retryPipeline = new ResiliencePipelineBuilder()
                         .AddRetry(new()
                         {
                             ShouldHandle = new PredicateBuilder()
                                            .Handle<InvalidOperationException>()
                                            .Handle<IOException>()
                                            .Handle<TimeoutException>()
                                            .Handle<TaskCanceledException>(),
                             MaxRetryAttempts = retryOptions.MaxAttempts,
                             Delay = retryOptions.InitialDelay,
                             BackoffType = retryOptions.BackoffType.Equals("Linear", StringComparison.OrdinalIgnoreCase)
                                               ? DelayBackoffType.Linear
                                               : DelayBackoffType.Exponential,
                             UseJitter = retryOptions.UseJitter,
                             OnRetry = args =>
                             {
                                 if (_logger is not null && _logger.IsEnabled(LogLevel.Warning))
                                 {
                                     _logger.LogWarning("重试 IPC 操作，尝试次数: {AttemptNumber}, 异常: {Exception}", args.AttemptNumber, args.Outcome.Exception?.Message);
                                 }
                                 return ValueTask.CompletedTask;
                             }
                         })
                         .AddTimeout(_options.Timeout.Business)
                         .Build();
        var circuitBreakerOptions = _options.CircuitBreaker;
        _circuitBreakerPipeline = new ResiliencePipelineBuilder()
                                  .AddCircuitBreaker(new()
                                  {
                                      ShouldHandle = new PredicateBuilder()
                                                     .Handle<InvalidOperationException>()
                                                     .Handle<IOException>()
                                                     .Handle<TimeoutException>(),
                                      FailureRatio = circuitBreakerOptions.FailureRatio,
                                      MinimumThroughput = circuitBreakerOptions.MinimumThroughput,
                                      BreakDuration = circuitBreakerOptions.BreakDuration,
                                      OnOpened = _ =>
                                      {
                                          if (_logger is not null && _logger.IsEnabled(LogLevel.Error))
                                          {
                                              _logger?.LogError("IPC 操作熔断器已打开，暂停操作 {BreakDuration}", circuitBreakerOptions.BreakDuration);
                                          }
                                          return ValueTask.CompletedTask;
                                      },
                                      OnClosed = _ =>
                                      {
                                          _logger?.LogInformation("IPC 操作熔断器已关闭，恢复操作");
                                          return ValueTask.CompletedTask;
                                      }
                                  })
                                  .Build();
        _logger?.LogInformation("IPC 客户端已初始化");
    }

    /// <inheritdoc />
    public async Task<IpcCommandResponse?> SendCommandAsync(IpcCommand command, TimeSpan timeout = default)
    {
        if (timeout == TimeSpan.Zero)
        {
            timeout = _options.DefaultTimeout;
        }
        try
        {
            return await _circuitBreakerPipeline.ExecuteAsync(async token => await _retryPipeline.ExecuteAsync(async _ =>
            {
                var response = await _commandService.SendAndReceiveAsync(command, timeout);
                if (response is not null)
                {
                    return response;
                }
                if (_logger is not null && _logger.IsEnabled(LogLevel.Error))
                {
                    _logger?.LogError("IPC 命令响应为空: {CommandType}, CommandId: {CommandId}", command.CommandType, command.CommandId);
                }
                throw new InvalidOperationException("未收到响应");
            }, token));
        }
        catch (Exception ex)
        {
            if (_logger is not null && _logger.IsEnabled(LogLevel.Error))
            {
                _logger?.LogError(ex, "执行 IPC 命令时发生错误: {CommandType}, CommandId: {CommandId}", command.CommandType, command.CommandId);
            }
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
    }
}
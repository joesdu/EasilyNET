using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Serializers;

namespace EasilyNET.Ipc.Models;

/// <summary>
/// Represents the configuration options for inter-process communication (IPC).
/// </summary>
/// <remarks>
/// This class provides various settings for configuring IPC, including options for circuit breakers,
/// timeouts, pipe names, Unix socket paths, server instance limits, client pipe pooling, serialization,  and retry
/// policies. It is typically used to customize IPC behavior in applications that rely on  inter-process communication
/// mechanisms.
/// </remarks>
public class IpcOptions
{
    /// <summary>
    /// Represents the configuration section name for inter-process communication (IPC) settings.
    /// </summary>
    /// <remarks>
    /// This constant is typically used to identify the "Ipc" section in configuration files or
    /// settings.
    /// </remarks>
    public const string SectionName = "Ipc";

    /// <summary>
    /// Gets or sets the configuration options for the circuit breaker.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Gets or sets the default timeout duration for operations.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the name of the pipe used for inter-process communication.
    /// </summary>
    public string PipeName { get; set; } = "Crazy_EasilyNETIpcPipe";

    /// <summary>
    /// Gets or sets the file system path to the Unix domain socket used for inter-process communication.
    /// </summary>
    /// <remarks>
    /// This property specifies the location of the Unix socket file. Ensure the path is accessible and
    /// writable by the application. Modifying this value may require corresponding changes in any clients or services that
    /// connect to the socket.
    /// </remarks>
    public string UnixSocketPath { get; set; } = "/tmp/easily.net.ipc.sock";

    /// <summary>
    /// Gets or sets the maximum number of server instances that can be created.
    /// </summary>
    public int MaxServerInstances { get; set; } = 4;

    /// <summary>
    /// 服务端传输层实例数量
    /// </summary>
    public int TransportCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of client pipe connections that can be pooled for reuse.
    /// </summary>
    /// <remarks>
    /// Increasing this value may improve performance in scenarios with high connection reuse,  but it could
    /// also increase resource usage. Set this value based on the expected workload  and available system
    /// resources.
    /// </remarks>
    public int ClientPipePoolSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the type of serializer to be used.
    /// </summary>
    public IIpcGenericSerializer? Serializer { get; set; } = new AdvancedJsonIpcSerializer();

    /// <summary>
    /// Gets or sets the retry policy options for handling transient failures during operations.
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout configuration options for operations.
    /// </summary>
    public TimeoutOptions Timeout { get; set; } = new();
}

/// <summary>
/// Represents configuration options for a retry policy, including backoff strategy, delays, and retry limits.
/// </summary>
/// <remarks>
/// This class is used to configure the behavior of retry mechanisms in scenarios where operations may
/// fail and need to be retried. It allows customization of the backoff type, initial delay, maximum retry attempts, and
/// whether to apply jitter to avoid retry collisions.
/// </remarks>
public class RetryPolicyOptions
{
    /// <summary>
    /// Gets or sets the type of backoff strategy to use for retry operations.
    /// </summary>
    public string BackoffType { get; set; } = "Exponential";

    /// <summary>
    /// Gets or sets the initial delay before the operation starts.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum number of attempts allowed for an operation.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether jitter should be applied to retry intervals.
    /// </summary>
    /// <remarks>
    /// Enabling jitter can help prevent synchronized retries in distributed systems, reducing the
    /// likelihood of contention or cascading failures.
    /// </remarks>
    public bool UseJitter { get; set; } = true;
}

/// <summary>
/// Represents the configuration options for a circuit breaker mechanism.
/// </summary>
/// <remarks>
/// A circuit breaker is used to prevent repeated execution of operations that are likely to fail,
/// allowing the system to recover and avoid excessive resource usage. These options control the conditions under which
/// the circuit breaker transitions between states.
/// </remarks>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the duration of the circuit breaker’s open state before it transitions to a half-open state.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the failure ratio threshold for the operation.
    /// </summary>
    public double FailureRatio { get; set; } = 0.8;

    /// <summary>
    /// Gets or sets the minimum throughput value required for the operation.
    /// </summary>
    public int MinimumThroughput { get; set; } = 5;
}

/// <summary>
/// Represents configuration options for timeout durations used in various operations.
/// </summary>
/// <remarks>
/// This class provides properties to configure timeout values for specific scenarios, such as business
/// logic operations and inter-process communication (IPC). These values can be adjusted to meet the requirements of the
/// application.
/// </remarks>
public class TimeoutOptions
{
    /// <summary>
    /// Gets or sets the duration of the business operation timeout.
    /// </summary>
    public TimeSpan Business { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the inter-process communication (IPC) timeout duration.
    /// </summary>
    public TimeSpan Ipc { get; set; } = TimeSpan.FromSeconds(10);
}
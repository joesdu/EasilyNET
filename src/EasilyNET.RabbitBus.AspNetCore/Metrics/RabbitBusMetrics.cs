using System.Diagnostics.Metrics;
using System.Reflection;

namespace EasilyNET.RabbitBus.AspNetCore.Metrics;

/// <summary>
/// RabbitMQ 事件总线 Metrics 定义
/// </summary>
internal static class RabbitBusMetrics
{
    private static readonly Meter Meter = new("EasilyNET.RabbitBus", GetVersion());

    // 发布相关
    public static readonly Counter<long> PublishedNormal = Meter.CreateCounter<long>("rabbitmq_published_normal_total", description: "Total normal events published");
    public static readonly Counter<long> PublishedDelayed = Meter.CreateCounter<long>("rabbitmq_published_delayed_total", description: "Total delayed events published");
    public static readonly Counter<long> PublishedBatch = Meter.CreateCounter<long>("rabbitmq_published_batch_events_total", description: "Total events published in batch mode");

    // Publisher Confirm
    public static readonly Counter<long> ConfirmAck = Meter.CreateCounter<long>("rabbitmq_publisher_ack_total", description: "Total publisher confirms acknowledged");
    public static readonly Counter<long> ConfirmNack = Meter.CreateCounter<long>("rabbitmq_publisher_nack_total", description: "Total publisher confirms negatively acknowledged");
    public static readonly Counter<long> ConfirmTimeout = Meter.CreateCounter<long>("rabbitmq_publisher_confirm_timeout_total", description: "Total publisher confirm timeouts");
    public static readonly UpDownCounter<long> OutstandingConfirms = Meter.CreateUpDownCounter<long>("rabbitmq_outstanding_confirms", description: "Current outstanding publisher confirms count");

    // 重试相关
    public static readonly Counter<long> RetryEnqueued = Meter.CreateCounter<long>("rabbitmq_retry_enqueued_total", description: "Total messages enqueued for retry");
    public static readonly Counter<long> RetryAttempt = Meter.CreateCounter<long>("rabbitmq_retry_attempt_total", description: "Total retry attempts executed");
    public static readonly Counter<long> RetryDiscarded = Meter.CreateCounter<long>("rabbitmq_retry_discarded_total", description: "Total messages discarded after exceeding max retries");
    public static readonly Counter<long> RetryRescheduled = Meter.CreateCounter<long>("rabbitmq_retry_rescheduled_total", description: "Total messages rescheduled due to retry failure");

    // 连接相关
    public static readonly Counter<long> ConnectionReconnects = Meter.CreateCounter<long>("rabbitmq_connection_reconnect_total", description: "Total successful reconnections to RabbitMQ");

    // 死信消息计数
    public static readonly Counter<long> DeadLettered = Meter.CreateCounter<long>("rabbitmq_deadletter_total", description: "Total messages dead-lettered after exceeding retry policy");

    private static long _connectionState; // 0/1
    private static long _retryQueueDepth; // 深度

    private static string GetVersion()
    {
        try
        {
            var asm = typeof(RabbitBusMetrics).Assembly;
            // 优先使用 InformationalVersion (可包含预发布/commit 信息)
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                return info;
            }
            var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (!string.IsNullOrWhiteSpace(file))
            {
                return file;
            }
            var ver = asm.GetName().Version?.ToString();
            return string.IsNullOrWhiteSpace(ver) ? "0.0.0" : ver!;
        }
        catch
        {
            return "0.0.0";
        }
    }

    public static void SetConnectionState(bool connected) => Volatile.Write(ref _connectionState, connected ? 1 : 0);
    public static void SetRetryQueueDepth(long depth) => Volatile.Write(ref _retryQueueDepth, depth);
}
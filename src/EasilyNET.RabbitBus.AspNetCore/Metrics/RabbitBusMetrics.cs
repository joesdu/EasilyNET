using System.Diagnostics.Metrics;
using System.Reflection;

namespace EasilyNET.RabbitBus.AspNetCore.Metrics;

/// <summary>
/// RabbitMQ 事件总线 Metrics 定义
/// </summary>
internal static class RabbitBusMetrics
{
    private static readonly Meter Meter = new("EasilyNET.RabbitBus", GetVersion());
    private static string s_appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown";
    private static bool s_connectionState;

    // 发布相关
    public static readonly Counter<long> PublishedNormal = Meter.CreateCounter<long>("rabbitmq.publish.normal.total", description: "Total normal events published");
    public static readonly Counter<long> PublishedDelayed = Meter.CreateCounter<long>("rabbitmq.publish.delayed.total", description: "Total delayed events published");
    public static readonly Counter<long> PublishedBatch = Meter.CreateCounter<long>("rabbitmq.publish.batch.total", description: "Total events published in batch mode");
    public static readonly Counter<long> PublishRetried = Meter.CreateCounter<long>("rabbitmq.publish.retried.total", description: "Total messages successfully re-published from the retry queue");
    public static readonly Counter<long> PublishDiscarded = Meter.CreateCounter<long>("rabbitmq.publish.discarded.total", description: "Total messages discarded from the retry queue after exceeding max retries");

    // Publisher Confirm
    public static readonly Counter<long> ConfirmAck = Meter.CreateCounter<long>("rabbitmq.publish.confirm.ack.total", description: "Total publisher confirms acknowledged");
    public static readonly Counter<long> ConfirmNack = Meter.CreateCounter<long>("rabbitmq.publish.confirm.nack.total", description: "Total publisher confirms negatively acknowledged");
    public static readonly Counter<long> ConfirmTimeout = Meter.CreateCounter<long>("rabbitmq.publish.confirm.timeout.total", description: "Total publisher confirm timeouts");
    public static readonly UpDownCounter<long> OutstandingConfirms = Meter.CreateUpDownCounter<long>("rabbitmq.publish.outstanding.confirms", description: "Current outstanding publisher confirms count");

    // 重试相关
    public static readonly Counter<long> RetryEnqueued = Meter.CreateCounter<long>("rabbitmq.retry.enqueued.total", description: "Total messages enqueued for retry");

    // 连接相关
    public static readonly Counter<long> ConnectionReconnects = Meter.CreateCounter<long>("rabbitmq.connection.reconnects.total", description: "Total successful reconnections to RabbitMQ");
    public static readonly UpDownCounter<long> ActiveConnections = Meter.CreateUpDownCounter<long>("rabbitmq.connection.active", description: "Current active RabbitMQ connections count");
    public static readonly UpDownCounter<long> ActiveChannels = Meter.CreateUpDownCounter<long>("rabbitmq.channel.active", description: "Current active RabbitMQ channels count");

    // 死信消息计数
    public static readonly Counter<long> DeadLettered = Meter.CreateCounter<long>("rabbitmq.deadletter.total", description: "Total messages dead-lettered after exceeding retry policy");

    static RabbitBusMetrics()
    {
        Meter.CreateObservableGauge("rabbitmq.connection.state",
            () => new Measurement<int>(s_connectionState ? 1 : 0, new KeyValuePair<string, object?>("client.name", s_appName)),
            "unit",
            "Connection state (1 for connected, 0 for disconnected)");
    }

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
            return string.IsNullOrWhiteSpace(ver) ? "0.0.0" : ver;
        }
        catch
        {
            return "0.0.0";
        }
    }

    public static void SetConnectionState(bool connected) => s_connectionState = connected;

    public static void SetAppName(string appName)
    {
        if (!string.IsNullOrWhiteSpace(appName))
        {
            s_appName = appName;
        }
    }
}
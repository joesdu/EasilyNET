using System.Diagnostics.Metrics;

namespace EasilyNET.RabbitBus.AspNetCore.Metrics;

/// <summary>
/// RabbitMQ 事件总线 Metrics 定义
/// </summary>
internal static class RabbitBusMetrics
{
    private static readonly Meter Meter = new("EasilyNET.RabbitBus", GetVersion());

    // 使用 AppDomain.CurrentDomain.FriendlyName 作为后备，避免依赖 Assembly.GetEntryAssembly()
    private static string s_appName = AppDomain.CurrentDomain.FriendlyName;
    private static bool s_connectionState;

    // 发布相关
    public static readonly Counter<long> PublishedNormal = Meter.CreateCounter<long>("rabbitmq.publish.normal.total", description: "Total normal events published");
    public static readonly Counter<long> PublishRetried = Meter.CreateCounter<long>("rabbitmq.publish.retried.total", description: "Total messages successfully re-published from the retry queue");
    public static readonly Counter<long> PublishDiscarded = Meter.CreateCounter<long>("rabbitmq.publish.discarded.total", description: "Total messages discarded from the retry queue after exceeding max retries");

    // Publisher Confirm
    public static readonly Counter<long> ConfirmAck = Meter.CreateCounter<long>("rabbitmq.publish.confirm.ack.total", description: "Total publisher confirms acknowledged");
    public static readonly Counter<long> ConfirmNack = Meter.CreateCounter<long>("rabbitmq.publish.confirm.nack.total", description: "Total publisher confirms negatively acknowledged");
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
        // 直接使用 AssemblyName.Version，避免使用 GetCustomAttribute 反射
        // 这在 AOT 环境下是安全的，因为它是直接读取元数据
        try
        {
            var ver = typeof(RabbitBusMetrics).Assembly.GetName().Version?.ToString();
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
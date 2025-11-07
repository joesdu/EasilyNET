using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.RabbitBus.Core.Enums;
using WebApi.Test.Unit.EventHandlers;
using WebApi.Test.Unit.Events;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// Rabbit服务注册 - 现代配置方式
/// </summary>
internal sealed class RabbitModule : AppModule
{
    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();
        context.Services.AddRabbitBus(c =>
        {
            c.WithConnection(f => f.Uri = new(config.GetConnectionString("Rabbit") ?? string.Empty))
             .WithConsumerSettings()
             .WithResilience()
             .WithExchangeSettings(false, true)
             .WithApplication("EasilyNET");

            // 配置自定义序列化器示例（可选）
            // c.WithSerializer<MsgPackSerializer>(); // 使用MessagePack序列化器
            // 或者使用实例:
            // c.WithSerializer(new MsgPackSerializer()); // 使用自定义序列化器

            // Delayed exchange example with two handlers, one ignored later
            c.AddEvent<HelloWorldEvent>(EModel.Delayed, "delayed.hello", "hello.world")
             .WithEventQos()
             .WithEventQueueArgs(new()
             {
                 ["x-message-ttl"] = 5000
             })
             .WithHandler<HelloWorldEventHandlers>()
             .WithHandler<DelayedEventHandlers>(); // will be ignored via IgnoreHandler below
            c.AddEvent<WorkQueuesEvent>(queueName: "work.queue")
             .ConfigureEvent(ec => ec.SequentialHandlerExecution = false)
             .WithEventQos(1000)
             .WithHandlerThreadCount(5)
             .WithHandler<WorkQueuesEventOneHandlers>();
            c.AddEvent<FanoutEventOne>(EModel.PublishSubscribe, "fanout_exchange", queueName: "fanout_queue1")
             .WithHandler<FanoutEventOneHandlers>();
            c.AddEvent<FanoutEventTwo>(EModel.PublishSubscribe, "fanout_exchange", queueName: "fanout_queue2")
             .WithHandler<FanoutEventTwoHandlers>();
            c.AddEvent<DirectEventOne>(EModel.Routing, "direct_exchange", "direct.queue1", "direct_queue1")
             .WithHandler<DirectEventOneHandlers>();
            c.AddEvent<DirectEventTwo>(EModel.Routing, "direct_exchange", "direct.queue2", "direct_queue2")
             .WithHandler<DirectEventTwoHandlers>();
            c.AddEvent<TopicEventOne>(EModel.Topics, "topic_exchange", "topic.queue.*", "topic_queue1")
             .WithHandler<TopicEventOneHandlers>();
            c.AddEvent<TopicEventTwo>(EModel.Topics, "topic_exchange", "topic.queue.1", "topic_queue2")
             .WithHandler<TopicEventTwoHandlers>();
            c.IgnoreHandler<HelloWorldEvent, DelayedEventHandlers>();
        });
        await Task.CompletedTask;
    }
}

/*
RabbitMQ 配置使用说明：

1. 基础配置：
   c.WithConnection(f => f.Uri = new(connectionString))
   c.WithConsumerSettings()
   c.WithResilience()
   c.WithApplication("YourAppName")

2. 交换机设置（新增功能）：
   c.WithExchangeSettings(
       skipExchangeDeclare: false,        // 是否跳过交换机声明
       validateExchangesOnStartup: true   // 是否在启动时验证交换机
   )

3. 自定义序列化器（新增功能）：
   c.WithSerializer<YourSerializer>()     // 使用泛型类型
   c.WithSerializer(new YourSerializer()) // 使用实例

4. 事件配置：
   c.AddEvent<YourEvent>(EModel.Direct, "exchange_name", "routing_key", "queue_name")
    .WithEventQos(prefetchCount: 100)
    .WithHandler<YourEventHandler>()
    .And()

4. 高级配置：
   - 跳过交换机声明：当交换机已预先创建且类型正确时使用
   - 启动时验证：确保所有配置的交换机存在且类型匹配
   - 批量处理：配置BatchSize来优化批量发布性能
   - 确认超时：调整ConfirmTimeoutMs来控制发布确认等待时间

配置验证将在控制台输出，显示所有关键配置项的值。
 */
/*
/// <summary>
/// MessagePackSerializer
/// </summary>
internal sealed class MsgPackSerializer : IBusSerializer
{
    private static readonly MessagePackSerializerOptions standardOptions =
        MessagePackSerializerOptions.Standard
                                    .WithResolver(CompositeResolver.Create(NativeDateTimeResolver.Instance, // 使用本地日期时间解析器
                                        ContractlessStandardResolver.Instance))                             // 使用无合约标准解析器
                                    .WithSecurity(MessagePackSecurity.UntrustedData);                       // 设置安全选项以处理不受信任的数据

    /// <summary>
    /// 使用 LZ4 算法对整个数组进行压缩.这种方式适用于需要对大量数据进行压缩的场景,压缩效率较高
    /// </summary>
    private static readonly MessagePackSerializerOptions lz4BlockArrayOptions =
        standardOptions.WithCompression(MessagePackCompression.Lz4BlockArray);

    /// <summary>
    /// 使用 LZ4 算法对每个数据块进行压缩.这种方式适用于需要对单个数据块进行压缩的场景,压缩速度较快
    /// </summary>
    private static readonly MessagePackSerializerOptions lz4BlockOptions =
        standardOptions.WithCompression(MessagePackCompression.Lz4Block);

    /// <inheritdoc />
    public byte[] Serialize(object? obj, Type type)
    {
        var data = MessagePackSerializer.Serialize(type, obj, standardOptions);
        var options = data.Length > 8192 ? lz4BlockArrayOptions : lz4BlockOptions;
        return MessagePackSerializer.Serialize(type, obj, options);
    }

    /// <inheritdoc />
    public object? Deserialize(byte[] data, Type type)
    {
        var options = data.Length > 8192 ? lz4BlockArrayOptions : lz4BlockOptions;
        return MessagePackSerializer.Deserialize(type, data, options);
    }
}
 */
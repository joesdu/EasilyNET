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
        // 现代配置方式：使用流畅的RabbitBusBuilder API
        // 所有配置都在一个地方集中管理，无需在事件类和处理器上添加属性
        var config = context.ServiceProvider.GetConfiguration();
        context.Services.AddRabbitBus(c =>
        {
            // 配置RabbitMQ连接
            c.WithConnection(f => f.Uri = new(config.GetConnectionString("Rabbit") ?? string.Empty));

            // 配置连接池和消费者设置
            c.WithConnectionPool().WithConsumerSettings();

            // 配置重试和弹性策略
            c.WithResilience();

            // 配置应用程序标识
            c.WithApplication("WebApi.Test.Unit");

            // ========== 事件配置 ==========
            // HelloWorldEvent - 延迟交换机配置
            c.AddEvent<HelloWorldEvent>(EModel.Delayed, "delayed.hello", "hello.world")
             .WithEventQos()
             .WithEventQueueArgs(new()
             {
                 ["x-message-ttl"] = 5000
             })
             .And();
            c.AddEvent<WorkQueuesEvent>(queueName: "work.queue")
             .WithEventQos(1000) // 增加预取数量以处理大量消息
             .ConfigureEvent(ec => ec.SequentialHandlerExecution = false)
             .And();

            // FanoutEventOne - 发布订阅模式配置
            c.AddEvent<FanoutEventOne>(EModel.PublishSubscribe, "fanout_exchange", queueName: "fanout_queue1");

            // FanoutEventTwo - 发布订阅模式配置
            c.AddEvent<FanoutEventTwo>(EModel.PublishSubscribe, "fanout_exchange", queueName: "fanout_queue2");

            // DirectEventOne - 路由模式配置
            c.AddEvent<DirectEventOne>(EModel.Routing, "direct_exchange", "direct.queue1", "direct_queue1");

            // DirectEventTwo - 路由模式配置
            c.AddEvent<DirectEventTwo>(EModel.Routing, "direct_exchange", "direct.queue2", "direct_queue2");

            // TopicEventOne - 主题模式配置
            c.AddEvent<TopicEventOne>(EModel.Topics, "topic_exchange", "topic.queue.*", "topic_queue1");

            // TopicEventTwo - 主题模式配置
            c.AddEvent<TopicEventTwo>(EModel.Topics, "topic_exchange", "topic.queue.1", "topic_queue2");
            c.IgnoreHandler<HelloWorldEvent, DelayedEventHandlers>();
        });
        await Task.CompletedTask;
    }
}

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
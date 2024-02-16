using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Extensions;
using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;
using EasilyNET.RabbitBus.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace EasilyNET.RabbitBus;

internal sealed class EventBus(IPersistentConnection conn, int retry, ISubscriptionsManager subsManager, ISubscriptionsManager deadManager, IServiceProvider sp, ILogger<EventBus> logger) : IBus, IDisposable
{
    private const string HandleName = nameof(IEventHandler<IEvent>.HandleAsync);

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private bool disposed;

    /// <inheritdoc />
    public async Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IEvent
    {
        if (!conn.IsConnected) await conn.TryConnect();
        var type = @event.GetType();
        logger.LogTrace("创建通道来发布事件: {EventId} ({EventName})", @event.EventId, type.Name);
        var info = type.GetCustomAttribute<ExchangeInfoAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(ExchangeInfoAttribute)}>,无法创建发布事件");
        if (!info.Enable) return;
        var channel = await conn.GetChannel();
        var properties = new BasicProperties
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            Priority = priority.GetValueOrDefault()
        };
        var headers = @event.GetHeaderAttributes();
        if (headers is not null && headers.Count is not 0) properties.Headers = headers;
        if (info is not { WorkModel: EModel.None })
        {
            var exchange_args = @event.GetExchangeArgAttributes();
            await channel.ExchangeDeclareAsync(info.ExchangeName, info.WorkModel.ToDescription(), true, arguments: exchange_args);
        }
        // 在发布事件前检查是否已经取消发布
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested) return;
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), jsonSerializerOptions);
        // 创建Policy规则
        var policy = Policy.Handle<BrokerUnreachableException>()
                           .Or<SocketException>()
                           .WaitAndRetry(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                               logger.LogError(ex, "无法发布事件: {EventId} 超时 {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message));
        await policy.Execute(async () =>
        {
            logger.LogTrace("发布事件: {EventId}", @event.EventId);
            await channel.BasicPublishAsync(info.ExchangeName, routingKey ?? info.RoutingKey, properties, body, true);
            await conn.ReturnChannel(channel);
        });
    }

    /// <inheritdoc />
    public async Task Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IEvent
    {
        if (!conn.IsConnected) await conn.TryConnect();
        var type = @event.GetType();
        logger.LogTrace("创建通道来发布事件: {EventId} ({EventName})", @event.EventId, type.Name);
        var info = type.GetCustomAttribute<ExchangeInfoAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(ExchangeInfoAttribute)}>,无法发布事件");
        if (!info.Enable) return;
        if (info is not { WorkModel: EModel.Delayed }) throw new($"延时队列的交换机类型必须为{nameof(EModel.Delayed)}");
        var channel = await conn.GetChannel();
        var properties = new BasicProperties
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            Priority = priority.GetValueOrDefault()
        };
        //延时时间从header赋值
        var headers = @event.GetHeaderAttributes();
        if (headers is not null)
        {
            var xDelay = headers.TryGetValue("x-delay", out var delay);
            headers["x-delay"] = xDelay && ttl == 0 && delay is not null ? delay : ttl;
            properties.Headers = headers;
        }
        else
        {
            properties.Headers?.Add("x-delay", ttl);
        }
        // x-delayed-type 必须加
        var exchange_args = @event.GetExchangeArgAttributes();
        if (exchange_args is not null)
        {
            var xDelayedType = exchange_args.TryGetValue("x-delayed-type", out var delayedType);
            exchange_args["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;
        }
        ////创建延时交换机,type类型为x-delayed-message
        await channel.ExchangeDeclareAsync(info.ExchangeName, info.WorkModel.ToDescription(), true, false, exchange_args);
        // 在发布事件前检查是否已经取消发布
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested) return;
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), jsonSerializerOptions);
        // 创建Policy规则
        var policy = Policy.Handle<BrokerUnreachableException>()
                           .Or<SocketException>()
                           .WaitAndRetry(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                               logger.LogError(ex, "无法发布事件: {EventId} 超时 {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message));
        await policy.Execute(async () =>
        {
            logger.LogTrace("发布事件: {EventId}", @event.EventId);
            await channel.BasicPublishAsync(info.ExchangeName, routingKey ?? info.RoutingKey, properties, body, true);
            await conn.ReturnChannel(channel);
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;
        subsManager.Clear();
        deadManager.Clear();
        disposed = true;
    }

    internal async Task Subscribe()
    {
        if (!conn.IsConnected) await conn.TryConnect();
        await InitialRabbit();
    }

    private async Task InitialRabbit()
    {
        var events = AssemblyHelper.FindTypes(o => o is { IsClass: true, IsAbstract: false } && o.IsBaseOn(typeof(Event)) && o.HasAttribute<ExchangeInfoAttribute>());
        foreach (var eventType in events)
        {
            var infoAttr = eventType.GetCustomAttribute<ExchangeInfoAttribute>()!;
            if (!infoAttr.Enable) continue;
            var dlx = eventType.GetCustomAttribute<DeadLetterExchangeInfoAttribute>();
            var eventName = subsManager.GetEventKey(eventType);
            await Task.Factory.StartNew(async () =>
            {
                using var channel = await CreateConsumerChannel(infoAttr, eventType, dlx);
                if (infoAttr is not { WorkModel: EModel.None })
                {
                    await DoInternalSubscription(eventName, infoAttr.ExchangeName, infoAttr, channel, false);
                }
                var handlers = AssemblyHelper.FindTypes(o => o is
                                                             {
                                                                 IsClass: true,
                                                                 IsAbstract: false
                                                             } &&
                                                             o.IsBaseOn(typeof(IEventHandler<>))).Select(s => s.GetTypeInfo());
                var handler = handlers.FirstOrDefault(o => o.ImplementedInterfaces.Any(s => s.GenericTypeArguments.Contains(eventType)));
                using var scope = sp.GetService<IServiceScopeFactory>()?.CreateScope();
                if (handler is null) return;
                var handle = scope?.ServiceProvider.GetService(handler);
                // 检查消费者是否已经注册,若是未注册则不启动消费.
                if (handle is null) return;
                subsManager.AddSubscription(eventType, handler);
                await StartBasicConsume(eventType, infoAttr, channel, false);
            }, TaskCreationOptions.LongRunning);
            if (dlx is not null)
            {
                await Task.Factory.StartNew(async () =>
                {
                    using var channel = await CreateConsumerChannel(infoAttr, eventType, dlx);
                    await DoInternalSubscription(eventName, dlx.ExchangeName, dlx, channel, true);
                    var xdl_handlers = AssemblyHelper.FindTypes(o => o is
                                                                     {
                                                                         IsClass: true,
                                                                         IsAbstract: false
                                                                     } &&
                                                                     o.IsBaseOn(typeof(IEventDeadLetterHandler<>))).Select(s => s.GetTypeInfo());
                    var handler = xdl_handlers.FirstOrDefault(o => o.ImplementedInterfaces.Any(s => s.GenericTypeArguments.Contains(eventType)));
                    if (handler is null) return;
                    using var scope = sp.GetService<IServiceScopeFactory>()?.CreateScope();
                    var handle = scope?.ServiceProvider.GetService(handler);
                    // 检查消费者是否已经注册,若是未注册则不启动消费.
                    if (handle is null) return;
                    deadManager.AddSubscription(eventType, handler);
                    await StartBasicConsume(eventType, dlx, channel, true);
                }, TaskCreationOptions.LongRunning);
            }
        }
    }

    private async Task<IChannel> CreateConsumerChannel(ExchangeInfoAttribute attr, Type eventType, DeadLetterExchangeInfoAttribute? dlx = null)
    {
        logger.LogTrace("创建消费者通道");
        var channel = await conn.GetChannel();
        var queue_args = eventType.GetQueueArgAttributes();
        if (dlx is not null && dlx.Enable)
        {
            queue_args ??= new Dictionary<string, object?>();
            queue_args.Add("x-dead-letter-exchange", dlx.ExchangeName);
            queue_args.Add("x-dead-letter-routing-key", dlx.RoutingKey);
            logger.LogTrace("创建死信消费者通道");
            var model = dlx.WorkModel is EModel.Delayed or EModel.None ? "direct" : dlx.WorkModel.ToDescription();
            //创建死信交换机
            await channel.ExchangeDeclareAsync(dlx.ExchangeName, model, true);
            //创建死信队列
            _ = await channel.QueueDeclareAsync(dlx.Queue, true, false, false);
        }
        if (attr is not { WorkModel: EModel.None })
        {
            var exchange_args = eventType.GetExchangeArgAttributes();
            if (exchange_args is not null)
            {
                var success = exchange_args.TryGetValue("x-delayed-type", out _);
                if (!success && attr is { WorkModel: EModel.Delayed }) exchange_args.Add("x-delayed-type", "direct"); //x-delayed-type必须加
            }
            //创建交换机
            await channel.ExchangeDeclareAsync(attr.ExchangeName, attr.WorkModel.ToDescription(), true, false, exchange_args);
        }
        //创建队列
        await channel.QueueDeclareAsync(attr.Queue, true, false, false, queue_args);
        channel.CallbackException += async (_, ea) =>
        {
            logger.LogWarning(ea.Exception, "重新创建消费者通道");
            subsManager.Clear();
            deadManager.Clear();
            await Subscribe();
        };
        return channel;
    }

    private async Task DoInternalSubscription(string eventName, string exchangeName, ExchangeAttribute attr, IChannel channel, bool isDeadLet)
    {
        var containsKey = isDeadLet ? deadManager.HasSubscriptionsForEvent(eventName) : subsManager.HasSubscriptionsForEvent(eventName);
        if (containsKey) return;
        if (!conn.IsConnected) await conn.TryConnect();
        await channel.QueueBindAsync(attr.Queue, exchangeName, attr.RoutingKey);
    }

    private async Task StartBasicConsume(Type eventType, ExchangeAttribute attr, IChannel channel, bool isDeadLet)
    {
        logger.LogTrace("启动消费者");
        var qos = eventType.GetCustomAttribute<QosAttribute>();
        if (qos is not null) await channel.BasicQosAsync(qos.PrefetchSize, qos.PrefetchCount, qos.Global);
        var consumer = new AsyncEventingBasicConsumer(channel);
        await channel.BasicConsumeAsync(attr.Queue, false, consumer);
        consumer.Received += async (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.Span);
            try
            {
                if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new InvalidOperationException($"假异常请求:{message}");
                }
                await ProcessEvent(eventType, message, isDeadLet, () => channel.BasicAck(ea.DeliveryTag, false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "错误处理消息:{Message}", message);
                // 先注释掉,若是大量消息重新入队,容易拖垮MQ,这里的处理办法是长时间不确认,所有消息由Unacked重新变为Ready
                //channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };
        while (true)
        {
            if (channel.IsClosed) break;
            await Task.Delay(100000);
        }
    }

    private async Task ProcessEvent(Type eventType, string message, bool isDeadLet, Action ack)
    {
        var eventName = isDeadLet ? deadManager.GetEventKey(eventType) : subsManager.GetEventKey(eventType);
        logger.LogTrace("处理事件: {EventName}", eventName);
        var policy = Policy.Handle<BrokerUnreachableException>()
                           .Or<SocketException>()
                           .WaitAndRetry(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                               logger.LogError(ex, "无法消费事件: {EventName} 超时 {Timeout}s ({ExceptionMessage})", eventName, $"{time.TotalSeconds:n1}", ex.Message));
        switch (isDeadLet)
        {
            case false when subsManager.HasSubscriptionsForEvent(eventName):
                await policy.Execute(async () =>
                {
                    var subscriptionTypes = subsManager.GetHandlersForEvent(eventName);
                    foreach (var subscriptionType in subscriptionTypes)
                    {
                        var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
                        await HandleMessage(eventType, message, concreteType, subscriptionType, ack);
                    }
                });
                break;
            case true when deadManager.HasSubscriptionsForEvent(eventName):
                await policy.Execute(async () =>
                {
                    var subscriptionTypes = deadManager.GetHandlersForEvent(eventName);
                    foreach (var subscriptionType in subscriptionTypes)
                    {
                        var concreteType = typeof(IEventDeadLetterHandler<>).MakeGenericType(eventType);
                        await HandleMessage(eventType, message, concreteType, subscriptionType, ack);
                    }
                });
                break;
            default:
                logger.LogError("没有订阅事件:{EventName}", eventName);
                break;
        }
    }

    private async Task HandleMessage(Type eventType, string message, Type concreteType, Type subscriptionType, Action ack)
    {
        var @event = JsonSerializer.Deserialize(message, eventType, jsonSerializerOptions);
        if (@event is null)
        {
            throw new($"集成事件{nameof(@event)}不能为空");
        }
        var method = concreteType.GetMethod(HandleName);
        if (method is null)
        {
            logger.LogError($"无法找到{nameof(@event)}事件处理器下处理方法");
            throw new($"无法找到{nameof(@event)}事件处理器下处理方法");
        }
        using var scope = sp.GetService<IServiceScopeFactory>()?.CreateScope();
        var handler = scope?.ServiceProvider.GetService(subscriptionType);
        if (handler is null) return;
        await Task.Yield();
        var obj = method.Invoke(handler, [@event]);
        if (obj is null) return;
        await (Task)obj;
        ack.Invoke();
    }
}
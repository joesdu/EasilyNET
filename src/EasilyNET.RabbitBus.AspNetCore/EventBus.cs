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
    public void Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IEvent
    {
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested) return;
        if (!conn.IsConnected) _ = conn.TryConnect();
        var type = @event.GetType();
        logger.LogTrace("创建通道来发布事件: {EventId} ({EventName})", @event.EventId, type.Name);
        var r_attr = type.GetCustomAttribute<ExchangeInfoAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(ExchangeInfoAttribute)}>,无法创建发布事件");
        if (!r_attr.Enable) return;
        var channel = conn.GetChannel();
        var properties = new BasicProperties
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            Priority = priority.GetValueOrDefault()
        };
        var headers = @event.GetHeaderAttributes();
        if (headers is not null && headers.Count is not 0) properties.Headers = headers;
        if (r_attr is not { WorkModel: EModel.None })
        {
            var exchange_args = @event.GetExchangeArgAttributes();
            channel.ExchangeDeclare(r_attr.ExchangeName, r_attr.WorkModel.ToDescription(), true, arguments: exchange_args);
        }
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), jsonSerializerOptions);
        // 创建Policy规则
        var policy = Policy.Handle<BrokerUnreachableException>()
                           .Or<SocketException>()
                           .WaitAndRetry(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                               logger.LogError(ex, "无法发布事件: {EventId} 超时 {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message));
        policy.Execute(async () =>
        {
            logger.LogTrace("发布事件: {EventId}", @event.EventId);
            await channel.BasicPublishAsync(r_attr.ExchangeName, routingKey ?? r_attr.RoutingKey, properties, body, true);
            conn.ReturnChannel(channel);
        });
    }

    /// <inheritdoc />
    public void Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IEvent
    {
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested) return;
        if (!conn.IsConnected) _ = conn.TryConnect();
        var type = @event.GetType();
        logger.LogTrace("创建通道来发布事件: {EventId} ({EventName})", @event.EventId, type.Name);
        var rabbitAttr = type.GetCustomAttribute<ExchangeInfoAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(ExchangeInfoAttribute)}>,无法发布事件");
        if (!rabbitAttr.Enable) return;
        if (rabbitAttr is not { WorkModel: EModel.Delayed }) throw new($"延时队列的交换机类型必须为{nameof(EModel.Delayed)}");
        var channel = conn.GetChannel();
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
        channel.ExchangeDeclare(rabbitAttr.ExchangeName, rabbitAttr.WorkModel.ToDescription(), true, false, exchange_args);
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), jsonSerializerOptions);
        // 创建Policy规则
        var policy = Policy.Handle<BrokerUnreachableException>()
                           .Or<SocketException>()
                           .WaitAndRetry(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                               logger.LogError(ex, "无法发布事件: {EventId} 超时 {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message));
        policy.Execute(async () =>
        {
            logger.LogTrace("发布事件: {EventId}", @event.EventId);
            await channel.BasicPublishAsync(rabbitAttr.ExchangeName, routingKey ?? rabbitAttr.RoutingKey, properties, body, true);
            conn.ReturnChannel(channel);
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

    internal void Subscribe()
    {
        if (!conn.IsConnected) _ = conn.TryConnect();
        InitialRabbit();
    }

    private void InitialRabbit()
    {
        var events = AssemblyHelper.FindTypes(o => o is { IsClass: true, IsAbstract: false } && o.IsBaseOn(typeof(Event)) && o.HasAttribute<ExchangeInfoAttribute>());
        foreach (var eventType in events)
        {
            var rabbitAttr = eventType.GetCustomAttribute<ExchangeInfoAttribute>()!;
            if (!rabbitAttr.Enable) continue;
            var xdlAttr = eventType.GetCustomAttribute<DeadLetterExchangeInfoAttribute>();
            var eventName = subsManager.GetEventKey(eventType);
            Task.Factory.StartNew(() =>
            {
                using var consumerChannel = CreateConsumerChannel(rabbitAttr, eventType, xdlAttr);
                if (rabbitAttr is not { WorkModel: EModel.None })
                {
                    DoInternalSubscription(eventName, rabbitAttr, consumerChannel);
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
                StartBasicConsume(eventType, rabbitAttr, consumerChannel);
            }, TaskCreationOptions.LongRunning);
            if (xdlAttr is not null)
            {
                Task.Factory.StartNew(() =>
                {
                    using var consumerChannel = CreateConsumerChannel(rabbitAttr, eventType, xdlAttr);
                    DoInternalSubscription(eventName, xdlAttr, consumerChannel);
                    var xdl_handlers = AssemblyHelper.FindTypes(o => o is
                                                                     {
                                                                         IsClass: true,
                                                                         IsAbstract: false
                                                                     } &&
                                                                     o.IsBaseOn(typeof(IEventDeadLetterHandler<>))).Select(s => s.GetTypeInfo());
                    var xdl_handler = xdl_handlers.FirstOrDefault(o => o.ImplementedInterfaces.Any(s => s.GenericTypeArguments.Contains(eventType)));
                    if (xdl_handler is null) return;
                    using var scope = sp.GetService<IServiceScopeFactory>()?.CreateScope();
                    var handle = scope?.ServiceProvider.GetService(xdl_handler);
                    // 检查消费者是否已经注册,若是未注册则不启动消费.
                    if (handle is null) return;
                    deadManager.AddSubscription(eventType, xdl_handler);
                    StartBasicConsume(eventType, xdlAttr, consumerChannel);
                }, TaskCreationOptions.LongRunning);
            }
        }
    }

    private IChannel CreateConsumerChannel(ExchangeInfoAttribute attr, Type eventType, DeadLetterExchangeInfoAttribute? xdlAttr = null)
    {
        logger.LogTrace("创建消费者通道");
        var channel = conn.GetChannel();
        var queue_args = eventType.GetQueueArgAttributes();
        if (xdlAttr is not null && xdlAttr.Enable)
        {
            queue_args ??= new Dictionary<string, object?>();
            queue_args.Add("x-dead-letter-exchange", xdlAttr.ExchangeName);
            queue_args.Add("x-dead-letter-routing-key", xdlAttr.RoutingKey);
            logger.LogTrace("创建死信消费者通道");
            var model = xdlAttr.WorkModel is EModel.Delayed or EModel.None ? "direct" : xdlAttr.WorkModel.ToDescription();
            //创建死信交换机
            channel.ExchangeDeclare(xdlAttr.ExchangeName, model, true);
            //创建死信队列
            _ = channel.QueueDeclare(xdlAttr.Queue, true, false, false);
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
            channel.ExchangeDeclare(attr.ExchangeName, attr.WorkModel.ToDescription(), true, false, exchange_args);
        }
        //创建队列
        _ = channel.QueueDeclare(attr.Queue, true, false, false, queue_args);
        channel.CallbackException += (_, ea) =>
        {
            logger.LogWarning(ea.Exception, "重新创建消费者通道");
            subsManager.Clear();
            deadManager.Clear();
            Subscribe();
        };
        return channel;
    }

    private void DoInternalSubscription(string eventName, ExchangeInfoAttribute attr, IChannel channel)
    {
        var containsKey = subsManager.HasSubscriptionsForEvent(eventName);
        if (containsKey) return;
        if (!conn.IsConnected) _ = conn.TryConnect();
        channel.QueueBind(attr.Queue, attr.ExchangeName, attr.RoutingKey);
    }

    private void DoInternalSubscription(string eventName, DeadLetterExchangeInfoAttribute attr, IChannel channel)
    {
        var containsKey = deadManager.HasSubscriptionsForEvent(eventName);
        if (containsKey) return;
        if (!conn.IsConnected) _ = conn.TryConnect();
        channel.QueueBind(attr.Queue, attr.ExchangeName, attr.RoutingKey);
    }

    private void StartBasicConsume(Type eventType, ExchangeInfoAttribute attr, IChannel channel)
    {
        logger.LogTrace("启动消费者");
        var qos = eventType.GetCustomAttribute<QosAttribute>();
        if (qos is not null) channel.BasicQos(qos.PrefetchSize, qos.PrefetchCount, qos.Global);
        var consumer = new AsyncEventingBasicConsumer(channel);
        _ = channel.BasicConsume(attr.Queue, false, consumer);
        consumer.Received += async (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.Span);
            try
            {
                if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new InvalidOperationException($"假异常请求:{message}");
                }
                await ProcessEvent(eventType, message, () => channel.BasicAck(ea.DeliveryTag, false));
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
            Thread.Sleep(100000);
        }
    }

    // ReSharper disable once ReplaceAsyncWithTaskReturn
    private async Task ProcessEvent(Type eventType, string message, Action ack)
    {
        var eventName = subsManager.GetEventKey(eventType);
        logger.LogTrace("处理事件: {EventName}", eventName);
        if (subsManager.HasSubscriptionsForEvent(eventName))
        {
            var policy = Policy.Handle<BrokerUnreachableException>()
                               .Or<SocketException>()
                               .WaitAndRetry(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                                   logger.LogError(ex, "无法消费事件: {EventName} 超时 {Timeout}s ({ExceptionMessage})", eventName, $"{time.TotalSeconds:n1}", ex.Message));
            await policy.Execute(async () =>
            {
                using var scope = sp.GetService<IServiceScopeFactory>()?.CreateScope();
                var subscriptionTypes = subsManager.GetHandlersForEvent(eventName);
                foreach (var subscriptionType in subscriptionTypes)
                {
                    var integrationEvent = JsonSerializer.Deserialize(message, eventType, jsonSerializerOptions);
                    var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
                    if (integrationEvent is null)
                    {
                        throw new($"集成事件{nameof(integrationEvent)}不能为空");
                    }
                    var method = concreteType.GetMethod(HandleName);
                    if (method is null)
                    {
                        logger.LogError($"无法找到{nameof(integrationEvent)}事件处理器下处理方法");
                        throw new($"无法找到{nameof(integrationEvent)}事件处理器下处理方法");
                    }
                    var handler = scope?.ServiceProvider.GetService(subscriptionType);
                    if (handler is null) continue;
                    await Task.Yield();
                    var obj = method.Invoke(handler, [integrationEvent]);
                    if (obj is null) continue;
                    await (Task)obj;
                    ack.Invoke();
                }
            });
        }
        else
        {
            logger.LogError("没有订阅事件:{EventName}", eventName);
        }
    }

    private void StartBasicConsume(Type eventType, DeadLetterExchangeInfoAttribute xdlAttr, IChannel channel)
    {
        logger.LogTrace("启动死信队列消费");
        var qos = eventType.GetCustomAttribute<QosAttribute>();
        if (qos is not null) channel.BasicQos(qos.PrefetchSize, qos.PrefetchCount, qos.Global);
        var consumer = new AsyncEventingBasicConsumer(channel);
        _ = channel.BasicConsume(xdlAttr.Queue, false, consumer);
        consumer.Received += async (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.Span);
            try
            {
                if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new InvalidOperationException($"假异常请求:{message}");
                }
                await ProcessDeadLetterEvent(eventType, message, () => channel.BasicAck(ea.DeliveryTag, false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "错误处理消息:{Message}", message);
            }
            // Even on exception we take the message off the queue.
            // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
            // For more information see: https://www.rabbitmq.com/dlx.html
        };
        while (true)
        {
            if (channel.IsClosed) break;
            Thread.Sleep(100000);
        }
    }

    // ReSharper disable once ReplaceAsyncWithTaskReturn
    private async Task ProcessDeadLetterEvent(Type eventType, string message, Action ack)
    {
        var eventName = deadManager.GetEventKey(eventType);
        logger.LogTrace("处理死信事件: {EventName}", eventName);
        if (deadManager.HasSubscriptionsForEvent(eventName))
        {
            var policy = Policy.Handle<BrokerUnreachableException>()
                               .Or<SocketException>()
                               .WaitAndRetry(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                                   logger.LogError(ex, "无法消费事件: {EventName} 超时 {Timeout}s ({ExceptionMessage})", eventName, $"{time.TotalSeconds:n1}", ex.Message));
            await policy.Execute(async () =>
            {
                using var scope = sp.GetService<IServiceScopeFactory>()?.CreateScope();
                var subscriptionTypes = deadManager.GetHandlersForEvent(eventName);
                foreach (var subscriptionType in subscriptionTypes)
                {
                    var integrationEvent = JsonSerializer.Deserialize(message, eventType, jsonSerializerOptions);
                    var concreteType = typeof(IEventDeadLetterHandler<>).MakeGenericType(eventType);
                    if (integrationEvent is null)
                    {
                        throw new($"集成事件{nameof(integrationEvent)}不能为空");
                    }
                    var method = concreteType.GetMethod(HandleName);
                    if (method is null)
                    {
                        logger.LogError($"无法找到{nameof(integrationEvent)}事件处理器下处理方法");
                        throw new($"无法找到{nameof(integrationEvent)}事件处理器下处理方法");
                    }
                    var handler = scope?.ServiceProvider.GetService(subscriptionType);
                    if (handler is null) continue;
                    await Task.Yield();
                    var obj = method.Invoke(handler, [integrationEvent]);
                    if (obj is null) continue;
                    await (Task)obj;
                    ack.Invoke();
                }
            });
        }
        else
        {
            logger.LogError("没有订阅死信事件:{EventName}", eventName);
        }
    }
}
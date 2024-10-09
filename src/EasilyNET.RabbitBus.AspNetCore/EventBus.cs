using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Enums;
using EasilyNET.RabbitBus.AspNetCore.Extensions;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;
using EasilyNET.RabbitBus.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore;

internal sealed class EventBus(IPersistentConnection conn, ISubscriptionsManager subsManager, IBusSerializer serializer, IServiceProvider sp, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pipelineProvider) : IBus
{
    private const string HandleName = nameof(IEventHandler<IEvent>.HandleAsync);
    private readonly ConcurrentDictionary<(Type HandlerType, Type EventType), Func<object, Task>?> _handleAsyncDelegateCache = [];

    public async Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IEvent
    {
        if (!conn.IsConnected) await conn.TryConnect();
        var type = @event.GetType();
        var exc = type.GetCustomAttribute<ExchangeAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(ExchangeAttribute)}>,无法创建消息");
        if (!exc.Enable) return;
        var channel = await conn.GetChannel();
        var properties = new BasicProperties
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            Priority = priority.GetValueOrDefault()
        };
        var headers = @event.GetHeaderAttributes();
        if (headers is not null && headers.Count is not 0) properties.Headers = headers;
        if (exc is not { WorkModel: EModel.None })
        {
            var exchange_args = @event.GetExchangeArgAttributes();
            await channel.ExchangeDeclareAsync(exc.ExchangeName, exc.WorkModel.ToDescription(), true, arguments: exchange_args);
        }
        // 在发布事件前检查是否已经取消发布
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested) return;
        var body = serializer.Serialize(@event, @event.GetType());
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            logger.LogTrace("发布: {EventId}", @event.EventId);
            await channel.BasicPublishAsync(exc.ExchangeName, routingKey ?? exc.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
            await conn.ReturnChannel(channel).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IEvent
    {
        if (!conn.IsConnected) await conn.TryConnect();
        var type = @event.GetType();
        var exc = type.GetCustomAttribute<ExchangeAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(ExchangeAttribute)}>,无法创建消息");
        if (!exc.Enable) return;
        if (exc is not { WorkModel: EModel.Delayed }) throw new($"延时队列的交换机类型必须为{nameof(EModel.Delayed)}");
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
        var exc_args = @event.GetExchangeArgAttributes();
        if (exc_args is not null)
        {
            var xDelayedType = exc_args.TryGetValue("x-delayed-type", out var delayedType);
            exc_args["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;
        }
        //创建延时交换机,type类型为x-delayed-message
        await channel.ExchangeDeclareAsync(exc.ExchangeName, exc.WorkModel.ToDescription(), true, false, exc_args);
        // 在发布事件前检查是否已经取消发布
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested) return;
        var body = serializer.Serialize(@event, @event.GetType());
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            logger.LogTrace("发布: {EventId}", @event.EventId);
            await channel.BasicPublishAsync(exc.ExchangeName, routingKey ?? exc.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
            await conn.ReturnChannel(channel).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    internal async Task Subscribe()
    {
        if (!conn.IsConnected) await conn.TryConnect();
        await InitialRabbit();
    }

    private async Task InitialRabbit()
    {
        var events = AssemblyHelper.FindTypes(o => o is { IsClass: true, IsAbstract: false } && o.IsBaseOn(typeof(IEvent)) && o.HasAttribute<ExchangeAttribute>());
        var handlers = AssemblyHelper.FindTypes(o => o is { IsClass: true, IsAbstract: false } &&
                                                     o.IsBaseOn(typeof(IEventHandler<>)) &&
                                                     !o.HasAttribute<IgnoreHandlerAttribute>()).Select(s => s.GetTypeInfo()).ToList();
        foreach (var @event in events)
        {
            var exc = @event.GetCustomAttribute<ExchangeAttribute>();
            if (exc is null || exc.Enable is false) continue;
            var handler = handlers.FindAll(o => o.ImplementedInterfaces.Any(s => s.GenericTypeArguments.Contains(@event)));
            if (handler.Count is not 0)
            {
                await Task.Factory.StartNew(async () =>
                {
                    await using var channel = await CreateConsumerChannel(exc, @event);
                    var handleKind = exc.WorkModel is EModel.Delayed ? EKindOfHandler.Delayed : EKindOfHandler.Normal;
                    if (exc is not { WorkModel: EModel.None })
                    {
                        if (subsManager.HasSubscriptionsForEvent(@event.Name, handleKind)) return;
                        if (!conn.IsConnected) await conn.TryConnect();
                        await channel.QueueBindAsync(exc.Queue, exc.ExchangeName, exc.RoutingKey);
                    }
                    subsManager.AddSubscription(@event, handleKind, handler);
                    await StartBasicConsume(@event, exc, channel);
                }, TaskCreationOptions.LongRunning);
            }
        }
    }

    private async Task<IChannel> CreateConsumerChannel(ExchangeAttribute exc, Type @event)
    {
        logger.LogTrace("创建消费者通道");
        var channel = await conn.GetChannel();
        var queue_args = @event.GetQueueArgAttributes();
        if (exc is not { WorkModel: EModel.None })
        {
            var exchange_args = @event.GetExchangeArgAttributes();
            if (exchange_args is not null)
            {
                var success = exchange_args.TryGetValue("x-delayed-type", out _);
                if (!success && exc is { WorkModel: EModel.Delayed }) exchange_args.Add("x-delayed-type", "direct"); //x-delayed-type必须加
            }
            //创建交换机
            await channel.ExchangeDeclareAsync(exc.ExchangeName, exc.WorkModel.ToDescription(), true, false, exchange_args);
        }
        //创建队列
        await channel.QueueDeclareAsync(exc.Queue, true, false, false, queue_args);
        channel.CallbackExceptionAsync += async (_, ea) =>
        {
            logger.LogWarning(ea.Exception, "重新创建消费者通道");
            subsManager.ClearSubscriptions();
            _handleAsyncDelegateCache.Clear();
            await Subscribe();
        };
        return channel;
    }

    private async Task StartBasicConsume(Type eventType, ExchangeAttribute exc, IChannel channel)
    {
        var handleKind = exc.WorkModel is EModel.Delayed ? EKindOfHandler.Delayed : EKindOfHandler.Normal;
        if (subsManager.HasSubscriptionsForEvent(eventType.Name, handleKind))
        {
            // Qos的配置通常和消费者的能力有关,所以这里从消费者的Handler中获取Qos特性
            // 一个程序通常对同一个事件只有一个Handler,所以这里只取第一个消费者的Qos
            var handlerType = subsManager.GetHandlersForEvent(eventType.Name, handleKind).FirstOrDefault(c => c.HasAttribute<QosAttribute>());
            var qos = handlerType?.GetCustomAttribute<QosAttribute>();
            if (qos is not null) await channel.BasicQosAsync(qos.PrefetchSize, qos.PrefetchCount, qos.Global);
        }
        var consumer = new AsyncEventingBasicConsumer(channel);
        await channel.BasicConsumeAsync(exc.Queue, false, consumer);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await ProcessEvent(eventType, ea.Body.Span.ToArray(), handleKind, async () => await channel.BasicAckAsync(ea.DeliveryTag, false).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "消息处理发生错误,DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                // 先注释掉,若是消费者没写对,造成大量消息重新入队,容易拖垮MQ,这里的处理办法是长时间不确认,所有消息由Unacked重新变为Ready
                //channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };
        while (true)
        {
            if (channel.IsClosed) break;
            await Task.Delay(100000);
        }
    }

    private async Task ProcessEvent(Type eventType, byte[] message, EKindOfHandler handleKind, Func<ValueTask> ack)
    {
        logger.LogTrace("处理事件: {EventName}", eventType.Name);
        if (subsManager.HasSubscriptionsForEvent(eventType.Name, handleKind))
        {
            var @event = serializer.Deserialize(message, eventType);
            var handlerTypes = subsManager.GetHandlersForEvent(eventType.Name, handleKind);
            using var scope = sp.GetService<IServiceScopeFactory>()?.CreateScope();
            var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
            foreach (var handlerType in handlerTypes)
            {
                var key = (handlerType, eventType);
                if (!_handleAsyncDelegateCache.TryGetValue(key, out var cachedDelegate))
                {
                    var method = handlerType.GetMethod(HandleName, [eventType]);
                    if (method is null)
                    {
                        logger.LogError($"无法找到{nameof(@event)}事件处理器");
                        return; // 或者抛出异常
                    }
                    var handler = scope?.ServiceProvider.GetService(handlerType);
                    if (handler is null) return;
                    var handleAsyncDelegate = CreateHandleAsyncDelegate(handler, method, eventType);
                    _handleAsyncDelegateCache[key] = handleAsyncDelegate;
                    cachedDelegate = handleAsyncDelegate;
                }
                if (cachedDelegate is not null && @event is not null)
                {
                    await pipeline.ExecuteAsync(async _ =>
                    {
                        await cachedDelegate(@event);
                        await ack.Invoke();
                    }).ConfigureAwait(false);
                }
            }
        }
        else
        {
            logger.LogError("没有订阅事件:{EventName}", eventType.Name);
        }
    }

    private static Func<object, Task> CreateHandleAsyncDelegate(object handler, MethodInfo method, Type eventType)
    {
        var delegateType = typeof(Func<,>).MakeGenericType(eventType, typeof(Task));
        var handleAsyncDelegate = Delegate.CreateDelegate(delegateType, handler, method);
        return async @event =>
        {
            if (handleAsyncDelegate.DynamicInvoke(@event) is Task task)
            {
                await task.ConfigureAwait(false);
            }
        };
    }
}
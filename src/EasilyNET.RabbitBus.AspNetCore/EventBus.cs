using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Enums;
using EasilyNET.RabbitBus.AspNetCore.Extensions;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;
using EasilyNET.RabbitBus.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore;

internal sealed class EventBus(PersistentConnection conn, ISubscriptionsManager subsManager, IBusSerializer serializer, IServiceProvider sp, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pipelineProvider) : IBus
{
#pragma warning disable IDE0340 // Remove redundant assignment
    // ReSharper disable once RedundantTypeArgumentsInsideNameof
    private const string HandleName = nameof(IEventHandler<IEvent>.HandleAsync);
#pragma warning restore IDE0340 // Remove redundant assignment
    private readonly ConcurrentDictionary<(Type HandlerType, Type EventType), Func<object, Task>?> _handleAsyncDelegateCache = [];

    public async Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IEvent
    {
        //if (!conn.IsConnected) await conn.TryConnect();
        var type = @event.GetType();
        var exc = type.GetCustomAttribute<ExchangeAttribute>() ??
                  throw new InvalidOperationException($"The event '{@event.GetType().Name}' is missing the required ExchangeAttribute. Unable to create the message.");
        if (!exc.Enable)
        {
            return;
        }
        var channel = conn.Channel;
        var properties = new BasicProperties
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            Priority = priority.GetValueOrDefault()
        };
        var headers = @event.GetHeaderAttributes();
        if (headers is not null && headers.Count is not 0)
        {
            properties.Headers = headers;
        }
        if (exc is not { WorkModel: EModel.None })
        {
            var exchangeArgs = @event.GetExchangeArgAttributes();
            await channel.ExchangeDeclareAsync(exc.ExchangeName, exc.WorkModel.ToDescription(), true, arguments: exchangeArgs);
        }
        // 在发布事件前检查是否已经取消发布
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested)
        {
            return;
        }
        var body = serializer.Serialize(@event, @event.GetType());
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Publishing event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
            }
            await channel.BasicPublishAsync(exc.ExchangeName, routingKey ?? exc.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
            //await conn.ReturnChannel(channel).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IEvent
    {
        //if (!conn.IsConnected) await conn.TryConnect();
        var type = @event.GetType();
        var exc = type.GetCustomAttribute<ExchangeAttribute>() ??
                  throw new InvalidOperationException($"The event '{@event.GetType().Name}' is missing the required ExchangeAttribute. Unable to create the message.");
        if (!exc.Enable)
        {
            return;
        }
        if (exc is not { WorkModel: EModel.Delayed })
        {
            throw new InvalidOperationException($"The exchange type for the delayed queue must be '{nameof(EModel.Delayed)}'. Event: '{@event.GetType().Name}'");
        }
        var channel = conn.Channel;
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
            properties.Headers = new Dictionary<string, object?> { { "x-delay", ttl } };
        }
        // x-delayed-type 必须加
        var excArgs = @event.GetExchangeArgAttributes();
        if (excArgs is not null)
        {
            var xDelayedType = excArgs.TryGetValue("x-delayed-type", out var delayedType);
            excArgs["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;
        }
        //创建延时交换机,type类型为x-delayed-message
        await channel.ExchangeDeclareAsync(exc.ExchangeName, exc.WorkModel.ToDescription(), true, false, excArgs);
        // 在发布事件前检查是否已经取消发布
        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested)
        {
            return;
        }
        var body = serializer.Serialize(@event, @event.GetType());
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Publishing event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
            }
            await channel.BasicPublishAsync(exc.ExchangeName, routingKey ?? exc.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
            //await conn.ReturnChannel(channel).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task Publish<T>(T @event, TimeSpan ttl, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IEvent
    {
        var realTtl = ttl.TotalMilliseconds.ConvertTo<uint>();
        await Publish(@event, realTtl, routingKey, priority, cancellationToken);
    }

    internal async Task RunRabbit()
    {
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
            if (exc is null || exc.Enable is false)
            {
                continue;
            }
            var handler = handlers.FindAll(o => o.ImplementedInterfaces.Any(s => s.GenericTypeArguments.Contains(@event)));
            if (handler.Count is not 0)
            {
                await Task.Factory.StartNew(async () =>
                {
                    await using var channel = await CreateConsumerChannel(exc, @event);
                    var handleKind = exc.WorkModel is EModel.Delayed ? EKindOfHandler.Delayed : EKindOfHandler.Normal;
                    if (exc is not { WorkModel: EModel.None })
                    {
                        if (subsManager.HasSubscriptionsForEvent(@event.Name, handleKind))
                        {
                            return;
                        }
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
        logger.LogTrace("Creating consumer channel");
        var channel = conn.Channel;
        var queueArgs = @event.GetQueueArgAttributes();
        await DeclareExchangeIfNeeded(exc, @event, channel);
        await channel.QueueDeclareAsync(exc.Queue, true, false, false, queueArgs);
        channel.CallbackExceptionAsync += async (_, ea) =>
        {
            logger.LogWarning(ea.Exception, "Recreating consumer channel");
            subsManager.ClearSubscriptions();
            _handleAsyncDelegateCache.Clear();
            await RunRabbit();
        };
        return channel;
    }

    private static async Task DeclareExchangeIfNeeded(ExchangeAttribute exc, Type @event, IChannel channel)
    {
        if (exc is not { WorkModel: EModel.None })
        {
            var exchangeArgs = @event.GetExchangeArgAttributes();
            AddDelayedTypeIfNeeded(exc, exchangeArgs);
            await channel.ExchangeDeclareAsync(exc.ExchangeName, exc.WorkModel.ToDescription(), true, false, exchangeArgs);
        }
    }

    private static void AddDelayedTypeIfNeeded(ExchangeAttribute exc, IDictionary<string, object?>? exchangeArgs)
    {
        if (exchangeArgs is not null && exc.WorkModel == EModel.Delayed)
        {
            exchangeArgs.TryAdd("x-delayed-type", "direct");
        }
    }

    private async Task StartBasicConsume(Type eventType, ExchangeAttribute exc, IChannel channel)
    {
        var handleKind = GetHandleKind(exc);
        if (subsManager.HasSubscriptionsForEvent(eventType.Name, handleKind))
        {
            await ConfigureQosIfNeeded(eventType, handleKind, channel);
        }
        var consumer = new AsyncEventingBasicConsumer(channel);
        await channel.BasicConsumeAsync(exc.Queue, false, consumer);
        consumer.ReceivedAsync += async (_, ea) => await HandleReceivedEvent(eventType, ea, handleKind, channel);
        await MonitorChannel(channel);
    }

    private static EKindOfHandler GetHandleKind(ExchangeAttribute exc) => exc.WorkModel == EModel.Delayed ? EKindOfHandler.Delayed : EKindOfHandler.Normal;

    private async Task ConfigureQosIfNeeded(Type eventType, EKindOfHandler handleKind, IChannel channel)
    {
        var handlerType = subsManager.GetHandlersForEvent(eventType.Name, handleKind).FirstOrDefault(c => c.HasAttribute<QosAttribute>());
        var qos = handlerType?.GetCustomAttribute<QosAttribute>();
        if (qos is not null)
        {
            await channel.BasicQosAsync(qos.PrefetchSize, qos.PrefetchCount, qos.Global);
        }
    }

    private async Task HandleReceivedEvent(Type eventType, BasicDeliverEventArgs ea, EKindOfHandler handleKind, IChannel channel)
    {
        try
        {
            await ProcessEvent(eventType, ea.Body.Span.ToArray(), handleKind, async () =>
            {
                try
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false).ConfigureAwait(false);
                }
                catch (ObjectDisposedException ex)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError(ex, "Channel was disposed before acknowledging message, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                    }
                    // 重新获取通道并重试
                    await RetryAcknowledgeMessage(ea);
                }
            });
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error processing message, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
            }
        }
    }

    private async Task RetryAcknowledgeMessage(BasicDeliverEventArgs ea)
    {
        try
        {
            var newChannel = conn.Channel;
            await newChannel.BasicAckAsync(ea.DeliveryTag, false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to acknowledge message after retry, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
            }
            // 重新执行 Subscribe 方法，重新初始化消费者和服务端的连接
            subsManager.ClearSubscriptions();
            _handleAsyncDelegateCache.Clear();
            await RunRabbit();
        }
    }

    private static async Task MonitorChannel(IChannel channel)
    {
        while (!channel.IsClosed)
        {
            await Task.Delay(100000);
        }
    }

    private async Task ProcessEvent(Type eventType, byte[] message, EKindOfHandler handleKind, Func<ValueTask> ack)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Processing event: {EventName}", eventType.Name);
        }
        if (!subsManager.HasSubscriptionsForEvent(eventType.Name, handleKind))
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("No subscriptions for event: {EventName}", eventType.Name);
            }
            return;
        }
        var @event = serializer.Deserialize(message, eventType);
        if (@event is null)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("Failed to deserialize event: {EventName}", eventType.Name);
            }
            return;
        }
        var handlerTypes = subsManager.GetHandlersForEvent(eventType.Name, handleKind);
        using var scope = sp.GetService<IServiceScopeFactory>()?.CreateScope();
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        foreach (var handlerType in handlerTypes)
        {
            var cachedDelegate = GetOrCreateHandlerDelegate(handlerType, eventType, scope);
            if (cachedDelegate is not null)
            {
                await pipeline.ExecuteAsync(async _ =>
                {
                    await cachedDelegate(@event);
                    await ack.Invoke();
                }).ConfigureAwait(false);
            }
        }
    }

    private Func<object, Task>? GetOrCreateHandlerDelegate(Type handlerType, Type eventType, IServiceScope? scope)
    {
        var key = (handlerType, eventType);
        // ReSharper disable once InvertIf
        if (!_handleAsyncDelegateCache.TryGetValue(key, out var cachedDelegate))
        {
            var method = handlerType.GetMethod(HandleName, [eventType]);
            if (method is null)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError("Handler method not found for event: {EventName}", eventType.Name);
                }
                return null;
            }
            var handler = scope?.ServiceProvider.GetService(handlerType);
            if (handler is null)
            {
                return null;
            }
            cachedDelegate = CreateHandleAsyncDelegate(handler, method, eventType);
            _handleAsyncDelegateCache[key] = cachedDelegate;
        }
        return cachedDelegate;
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
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

internal sealed record EventBus : IBus
{
    private const string HandleName = nameof(IEventHandler<>.HandleAsync);
    private readonly PersistentConnection _conn;
    private readonly ConcurrentDictionary<(Type HandlerType, Type EventType), Func<object, Task>?> _handleAsyncDelegateCache = [];
    private readonly ILogger<EventBus> _logger;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly IBusSerializer _serializer;
    private readonly IServiceProvider _sp;
    private readonly ISubscriptionsManager _subsManager;

    private CancellationTokenSource _cancellationTokenSource = new();

    public EventBus(PersistentConnection conn, ISubscriptionsManager sm, IBusSerializer ser, IServiceProvider sp, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pp)
    {
        _conn = conn;
        _subsManager = sm;
        _serializer = ser;
        _sp = sp;
        _logger = logger;
        _pipelineProvider = pp;
        // 订阅 PersistentConnection 的重连事件
        _conn.ConnectionReconnected += async (_, _) =>
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("RabbitMQ connection reconnected. Reinitializing consumers...");
            }
            _subsManager.ClearSubscriptions(); // 清除现有订阅
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new();
            await RunRabbit(); // 重新初始化消费者
        };

        // 订阅 PersistentConnection 的断开连接事件
        _conn.ConnectionDisconnected += (_, _) =>
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("RabbitMQ connection disconnected. Stopping consumers...");
            }
            _cancellationTokenSource.Cancel(); // 取消消费者线程
        };
    }

    public async Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var type = @event.GetType();
        var exc = type.GetCustomAttribute<ExchangeAttribute>() ??
                  throw new InvalidOperationException($"The event '{@event.GetType().Name}' is missing the required ExchangeAttribute. Unable to create the message.");
        if (!exc.Enable)
        {
            return;
        }
        var channel = _conn.Channel;
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
            await channel.ExchangeDeclareAsync(exc.ExchangeName, exc.WorkModel.Description, true, arguments: exchangeArgs, cancellationToken: cancellationToken);
        }
        var body = _serializer.Serialize(@event, @event.GetType());
        var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Publishing event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
            }
            await channel.BasicPublishAsync(exc.ExchangeName, routingKey ?? exc.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
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
        var channel = _conn.Channel;
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
        await channel.ExchangeDeclareAsync(exc.ExchangeName, exc.WorkModel.Description, true, false, excArgs, cancellationToken: cancellationToken);
        var body = _serializer.Serialize(@event, @event.GetType());
        var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Publishing event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
            }
            await channel.BasicPublishAsync(exc.ExchangeName, routingKey ?? exc.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task Publish<T>(T @event, TimeSpan ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
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
                    var ct = _cancellationTokenSource.Token;
                    await using var channel = await CreateConsumerChannel(exc, @event, ct);
                    var handleKind = exc.WorkModel is EModel.Delayed ? EKindOfHandler.Delayed : EKindOfHandler.Normal;
                    if (exc is not { WorkModel: EModel.None })
                    {
                        if (_subsManager.HasSubscriptionsForEvent(@event.Name, handleKind))
                        {
                            return;
                        }
                        await channel.QueueBindAsync(exc.Queue, exc.ExchangeName, exc.RoutingKey, cancellationToken: ct);
                    }
                    _subsManager.AddSubscription(@event, handleKind, handler);
                    await StartBasicConsume(@event, exc, channel, ct);
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
    }

    private async Task<IChannel> CreateConsumerChannel(ExchangeAttribute exc, Type @event, CancellationToken ct)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Creating consumer channel");
        }
        var channel = _conn.Channel;
        var queueArgs = @event.GetQueueArgAttributes();
        await DeclareExchangeIfNeeded(exc, @event, channel, ct);
        await channel.QueueDeclareAsync(exc.Queue, true, false, false, queueArgs, cancellationToken: ct);
        channel.CallbackExceptionAsync += async (_, ea) =>
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ea.Exception, "Recreating consumer channel");
            }
            _subsManager.ClearSubscriptions();
            _handleAsyncDelegateCache.Clear();
            await RunRabbit();
        };
        return channel;
    }

    private static async Task DeclareExchangeIfNeeded(ExchangeAttribute exc, Type @event, IChannel channel, CancellationToken ct)
    {
        if (exc is not { WorkModel: EModel.None })
        {
            var exchangeArgs = @event.GetExchangeArgAttributes();
            if (exchangeArgs is not null && exc.WorkModel == EModel.Delayed)
            {
                exchangeArgs.TryAdd("x-delayed-type", "direct");
            }
            await channel.ExchangeDeclareAsync(exc.ExchangeName, exc.WorkModel.Description, true, false, exchangeArgs, cancellationToken: ct);
        }
    }

    private async Task StartBasicConsume(Type eventType, ExchangeAttribute exc, IChannel channel, CancellationToken ct)
    {
        var handleKind = GetHandleKind(exc);
        if (_subsManager.HasSubscriptionsForEvent(eventType.Name, handleKind))
        {
            await ConfigureQosIfNeeded(eventType, handleKind, channel, ct);
        }
        var consumer = new AsyncEventingBasicConsumer(channel);
        await channel.BasicConsumeAsync(exc.Queue, false, consumer, ct);
        consumer.ReceivedAsync += async (_, ea) => await HandleReceivedEvent(eventType, ea, handleKind, channel, ct);
        while (!channel.IsClosed)
        {
            await Task.Delay(100000, ct);
        }
    }

    private static EKindOfHandler GetHandleKind(ExchangeAttribute exc) => exc.WorkModel == EModel.Delayed ? EKindOfHandler.Delayed : EKindOfHandler.Normal;

    private async Task ConfigureQosIfNeeded(Type eventType, EKindOfHandler handleKind, IChannel channel, CancellationToken ct)
    {
        var handlerType = _subsManager.GetHandlersForEvent(eventType.Name, handleKind).FirstOrDefault(c => c.HasAttribute<QosAttribute>());
        var qos = handlerType?.GetCustomAttribute<QosAttribute>();
        if (qos is not null)
        {
            await channel.BasicQosAsync(qos.PrefetchSize, qos.PrefetchCount, qos.Global, ct);
        }
    }

    private async Task HandleReceivedEvent(Type eventType, BasicDeliverEventArgs ea, EKindOfHandler handleKind, IChannel channel, CancellationToken ct)
    {
        try
        {
            await ProcessEvent(eventType, ea.Body.Span.ToArray(), handleKind, async () =>
            {
                try
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct).ConfigureAwait(false);
                }
                catch (ObjectDisposedException ex)
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _logger.LogError(ex, "Channel was disposed before acknowledging message, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                    }
                }
            }, ct);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error processing message, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
            }
        }
    }

    private async Task ProcessEvent(Type eventType, byte[] message, EKindOfHandler handleKind, Func<ValueTask> ack, CancellationToken ct)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Processing event: {EventName}", eventType.Name);
        }
        if (!_subsManager.HasSubscriptionsForEvent(eventType.Name, handleKind))
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError("No subscriptions for event: {EventName}", eventType.Name);
            }
            return;
        }
        var @event = _serializer.Deserialize(message, eventType);
        if (@event is null)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError("Failed to deserialize event: {EventName}", eventType.Name);
            }
            return;
        }
        var handlerTypes = _subsManager.GetHandlersForEvent(eventType.Name, handleKind);
        using var scope = _sp.GetService<IServiceScopeFactory>()?.CreateScope();
        var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        foreach (var handlerType in handlerTypes)
        {
            var cachedDelegate = GetOrCreateHandlerDelegate(handlerType, eventType, scope);
            if (cachedDelegate is not null)
            {
                await pipeline.ExecuteAsync(async _ =>
                {
                    await cachedDelegate(@event);
                    await ack.Invoke();
                }, ct).ConfigureAwait(false);
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
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError("Handler method not found for event: {EventName}", eventType.Name);
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
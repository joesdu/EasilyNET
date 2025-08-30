using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Enums;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore;

internal sealed record EventBus : IBus
{
    private const string HandleName = nameof(IEventHandler<>.HandleAsync);

    private readonly PersistentConnection _conn;
    private readonly EventConfigurationRegistry _eventRegistry;
    private readonly ConcurrentDictionary<(Type HandlerType, Type EventType), Func<object, Task>?> _handleAsyncDelegateCache = [];
    private readonly ILogger<EventBus> _logger;
    private readonly IOptionsMonitor<RabbitConfig> _options;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly IBusSerializer _serializer;
    private readonly IServiceProvider _sp;
    private readonly ISubscriptionsManager _subsManager;

    private CancellationTokenSource _cancellationTokenSource = new();

    public EventBus(PersistentConnection conn, ISubscriptionsManager sm, IBusSerializer ser, IServiceProvider sp, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pp, EventConfigurationRegistry eventRegistry, IOptionsMonitor<RabbitConfig> options)
    {
        _conn = conn;
        _subsManager = sm;
        _serializer = ser;
        _sp = sp;
        _logger = logger;
        _pipelineProvider = pp;
        _eventRegistry = eventRegistry;
        _options = options;

        // 订阅 PersistentConnection 的断开连接事件
        _conn.ConnectionDisconnected += (_, _) =>
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("RabbitMQ connection disconnected. Stopping consumers...");
            }
            _cancellationTokenSource.Cancel(); // 取消消费者线程
        };

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
    }

    public async Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var config = _eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
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

        // Use headers from configuration
        if (config.Headers.Count > 0)
        {
            properties.Headers = config.Headers;
        }

        // Declare exchange if needed
        if (config.Exchange.Type != EModel.None)
        {
            await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, config.Exchange.Arguments, cancellationToken: cancellationToken);
        }
        var body = _serializer.Serialize(@event, @event.GetType());
        var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Publishing event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
            }
            await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var config = _eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
        {
            return;
        }
        if (config.Exchange.Type != EModel.Delayed)
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

        // Handle headers with x-delay
        var headers = new Dictionary<string, object?>(config.Headers);
        var xDelay = headers.TryGetValue("x-delay", out var delay);
        headers["x-delay"] = xDelay && ttl == 0 && delay is not null ? delay : ttl;
        properties.Headers = headers;

        // Ensure x-delayed-type is set
        var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
        var xDelayedType = exchangeArgs.TryGetValue("x-delayed-type", out var delayedType);
        exchangeArgs["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;

        // Declare delayed exchange
        await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, exchangeArgs, cancellationToken: cancellationToken);
        var body = _serializer.Serialize(@event, @event.GetType());
        var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Publishing event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
            }
            await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
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
        var events = AssemblyHelper.FindTypes(o => o is { IsClass: true, IsAbstract: false } && o.IsBaseOn(typeof(IEvent)));
        var handlers = AssemblyHelper.FindTypes(o => o is { IsClass: true, IsAbstract: false } &&
                                                     o.IsBaseOn(typeof(IEventHandler<>))).Select(s => s.GetTypeInfo()).ToList();
        foreach (var @event in events)
        {
            var config = _eventRegistry.GetConfiguration(@event);
            if (config is null || !config.Enabled)
            {
                continue;
            }
            var handler = handlers.FindAll(o => o.ImplementedInterfaces.Any(s => s.GenericTypeArguments.Contains(@event)));
            if (handler.Count is 0)
            {
                continue;
            }
            // Filter out ignored handlers
            if (config.IgnoredHandlers.Count > 0)
            {
                handler = [.. handler.Where(h => !config.IgnoredHandlers.Contains(h.AsType()))];
            }
            if (handler.Count > 0)
            {
                await Task.Factory.StartNew(async () =>
                {
                    var ct = _cancellationTokenSource.Token;
                    await using var channel = await CreateConsumerChannel(config, ct);
                    var handleKind = config.Exchange.Type is EModel.Delayed ? EKindOfHandler.Delayed : EKindOfHandler.Normal;
                    if (config.Exchange.Type != EModel.None)
                    {
                        if (_subsManager.HasSubscriptionsForEvent(@event.Name, handleKind))
                        {
                            return;
                        }
                        await channel.QueueBindAsync(config.Queue.Name, config.Exchange.Name, config.Exchange.RoutingKey, cancellationToken: ct);
                    }
                    _subsManager.AddSubscription(@event, handleKind, handler);
                    await StartBasicConsume(@event, config, channel, ct);
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
    }

    private async Task<IChannel> CreateConsumerChannel(EventConfiguration config, CancellationToken ct)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Creating consumer channel");
        }
        var channel = _conn.Channel;
        await DeclareExchangeIfNeeded(config, channel, ct);
        await channel.QueueDeclareAsync(config.Queue.Name, config.Queue.Durable, config.Queue.Exclusive, config.Queue.AutoDelete, config.Queue.Arguments, cancellationToken: ct);
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

    private static async Task DeclareExchangeIfNeeded(EventConfiguration config, IChannel channel, CancellationToken ct)
    {
        if (config.Exchange.Type != EModel.None)
        {
            var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
            if (config.Exchange.Type == EModel.Delayed)
            {
                exchangeArgs.TryAdd("x-delayed-type", "direct");
            }
            await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, exchangeArgs, cancellationToken: ct);
        }
    }

    private async Task StartBasicConsume(Type eventType, EventConfiguration config, IChannel channel, CancellationToken ct)
    {
        var handleKind = GetHandleKind(config);
        if (_subsManager.HasSubscriptionsForEvent(eventType.Name, handleKind))
        {
            var rabbitConfig = _options.Get(Constant.OptionName);
            await ConfigureQosIfNeeded(config, channel, rabbitConfig.Qos, ct);
        }
        var consumer = new AsyncEventingBasicConsumer(channel);
        await channel.BasicConsumeAsync(config.Queue.Name, false, consumer, ct);
        consumer.ReceivedAsync += async (_, ea) => await HandleReceivedEvent(eventType, ea, handleKind, channel, ct);
        while (!channel.IsClosed)
        {
            await Task.Delay(100000, ct);
        }
    }

    private static EKindOfHandler GetHandleKind(EventConfiguration config) => config.Exchange.Type == EModel.Delayed ? EKindOfHandler.Delayed : EKindOfHandler.Normal;

    private static async Task ConfigureQosIfNeeded(EventConfiguration config, IChannel channel, QosConfig defaultQos, CancellationToken ct)
    {
        // Use event-specific QoS if configured, otherwise use default from RabbitConfig
        var qosToUse = config.Qos.PrefetchCount > 0 ? config.Qos : defaultQos;
        if (qosToUse.PrefetchCount > 0)
        {
            await channel.BasicQosAsync(qosToUse.PrefetchSize, qosToUse.PrefetchCount, qosToUse.Global, ct);
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
        var config = _eventRegistry.GetConfiguration(eventType);
        var sequentialExecution = config?.SequentialHandlerExecution ?? false;
        if (sequentialExecution)
        {
            // Execute handlers sequentially to maintain order
            foreach (var handlerType in handlerTypes)
            {
                var cachedDelegate = GetOrCreateHandlerDelegate(handlerType, eventType, scope);
                if (cachedDelegate is null)
                {
                    continue;
                }
                var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
                try
                {
                    await pipeline.ExecuteAsync(async _ => await cachedDelegate(@event), ct);
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _logger.LogError(ex, "Error executing handler {HandlerType} for event: {EventName}", handlerType.Name, eventType.Name);
                    }
                    throw; // Re-throw to prevent ACK
                }
            }
        }
        else
        {
            // Process handlers with controlled parallelism to prevent CPU overload
            var rabbitConfig = _options.Get(Constant.OptionName);
            var maxDegreeOfParallelism = rabbitConfig.HandlerMaxDegreeOfParallelism;

            // Create handler execution tasks
            var handlerExecutions = new List<(Type HandlerType, Func<Task> ExecutionTask)>();
            foreach (var handlerType in handlerTypes)
            {
                var cachedDelegate = GetOrCreateHandlerDelegate(handlerType, eventType, scope);
                if (cachedDelegate is null)
                {
                    continue;
                }

                // Create execution task with resilience pipeline
                var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
                handlerExecutions.Add((handlerType, executionTask));
                continue;
                async Task executionTask() => await pipeline.ExecuteAsync(async _ => await cachedDelegate(@event), ct);
            }

            // Execute handlers with controlled parallelism
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = ct
            };
            var exceptions = new ConcurrentBag<Exception>();
            await Parallel.ForEachAsync(handlerExecutions, parallelOptions, async (handlerExecution, _) =>
            {
                try
                {
                    await handlerExecution.ExecutionTask();
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _logger.LogError(ex, "Error executing handler {HandlerType} for event: {EventName}", handlerExecution.HandlerType.Name, eventType.Name);
                    }
                    exceptions.Add(ex);
                }
            });

            // If any handler failed, throw aggregate exception to prevent ACK
            if (!exceptions.IsEmpty)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError("Error processing handlers for event: {EventName}. Total errors: {ErrorCount}", eventType.Name, exceptions.Count);
                }
                throw new AggregateException("One or more event handlers failed", exceptions);
            }
        }

        // Only ACK if all handlers completed successfully
        await ack.Invoke();
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
        // Parameter names do not affect runtime performance, but are used in debugging, reflection, and tooling scenarios
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var eventParam = Expression.Parameter(typeof(object), "event");
        var convertedHandler = Expression.Convert(handlerParam, handler.GetType());
        var convertedEvent = Expression.Convert(eventParam, eventType);
        var methodCall = Expression.Call(convertedHandler, method, convertedEvent);
        var lambda = Expression.Lambda<Func<object, object, Task>>(methodCall, handlerParam, eventParam);
        var compiledDelegate = lambda.Compile();
        return async @event =>
        {
            var task = compiledDelegate(handler, @event);
            await task.ConfigureAwait(false);
        };
    }
}
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Configs;
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
    private readonly SemaphoreSlim _confirmSemaphore = new(1, 1);

    private readonly PersistentConnection _conn;

    private readonly ConcurrentDictionary<Type, List<Type>> _eventHandlerCache = [];
    private readonly EventConfigurationRegistry _eventRegistry;
    private readonly ConcurrentDictionary<(Type HandlerType, Type EventType), Func<object, Task>?> _handleAsyncDelegateCache = [];
    private readonly ILogger<EventBus> _logger;
    private readonly ConcurrentQueue<(IEvent Event, string? RoutingKey, byte? Priority, int RetryCount, DateTime NextRetryTime)> _nackedMessages = [];
    private readonly IOptionsMonitor<RabbitConfig> _options;

    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<bool>> _outstandingConfirms = [];
    private readonly ConcurrentDictionary<ulong, (IEvent Event, string? RoutingKey, byte? Priority, int RetryCount)> _outstandingMessages = [];
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly ConcurrentDictionary<Type, Func<EventBus, IEvent, string?, byte?, CancellationToken, Task>?> _publishDelegateCache = [];

    private readonly ConcurrentDictionary<Type, MethodInfo?> _publishMethodCache = [];
    private readonly IBusSerializer _serializer;
    private readonly IServiceProvider _sp;

    private CancellationTokenSource _cancellationTokenSource = new();

    public EventBus(PersistentConnection conn, IBusSerializer ser, IServiceProvider sp, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pp, EventConfigurationRegistry eventRegistry, IOptionsMonitor<RabbitConfig> options)
    {
        _conn = conn;
        _serializer = ser;
        _sp = sp;
        _logger = logger;
        _pipelineProvider = pp;
        _eventRegistry = eventRegistry;
        _options = options;
        _conn.ConnectionDisconnected += (_, _) =>
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("RabbitMQ connection disconnected. Stopping consumers...");
            }
            _cancellationTokenSource.Cancel();
        };
        _conn.ConnectionReconnected += async (_, _) =>
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("RabbitMQ connection reconnected. Reinitializing consumers...");
            }
            _eventHandlerCache.Clear();
            _handleAsyncDelegateCache.Clear();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new();
            await RunRabbit();
        };
        _conn.Channel.BasicAcksAsync += OnBasicAcks;
        _conn.Channel.BasicNacksAsync += OnBasicNacks;
        _conn.Channel.BasicReturnAsync += OnBasicReturn;
        _ = Task.Run(StartNackedMessageRetryTask, _cancellationTokenSource.Token);
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
        if (config.Headers.Count > 0)
        {
            properties.Headers = config.Headers;
        }
        if (config.Exchange.Type != EModel.None)
        {
            await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, config.Exchange.Arguments, cancellationToken: cancellationToken);
        }
        var sequenceNumber = await channel.GetNextPublishSequenceNumberAsync(cancellationToken);
        var tcs = new TaskCompletionSource<bool>();
        _outstandingConfirms[sequenceNumber] = tcs;
        _outstandingMessages[sequenceNumber] = (@event, routingKey, priority.GetValueOrDefault(), 0);
        var body = _serializer.Serialize(@event, @event.GetType());
        var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Publishing event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
            }
            await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        var rabbitConfig = _options.Get(Constant.OptionName);
        if (rabbitConfig.PublisherConfirms)
        {
            try
            {
                await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(rabbitConfig.ConfirmTimeoutMs), cancellationToken);
            }
            catch (TimeoutException)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Timeout waiting for publisher confirm for event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
                }
                throw;
            }
        }
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
        var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
        var xDelayedType = exchangeArgs.TryGetValue("x-delayed-type", out var delayedType);
        exchangeArgs["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;
        await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, exchangeArgs, cancellationToken: cancellationToken);
        var sequenceNumber = await channel.GetNextPublishSequenceNumberAsync(cancellationToken);
        var tcs = new TaskCompletionSource<bool>();
        _outstandingConfirms[sequenceNumber] = tcs;
        _outstandingMessages[sequenceNumber] = (@event, routingKey, priority.GetValueOrDefault(), 0);
        var body = _serializer.Serialize(@event, @event.GetType());
        var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Publishing delayed event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
            }
            await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        var rabbitConfig = _options.Get(Constant.OptionName);
        if (rabbitConfig.PublisherConfirms)
        {
            try
            {
                await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(rabbitConfig.ConfirmTimeoutMs), cancellationToken);
            }
            catch (TimeoutException)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Timeout waiting for publisher confirm for delayed event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
                }
                throw;
            }
        }
    }

    public async Task Publish<T>(T @event, TimeSpan ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await Publish(@event, (uint)ttl.TotalMilliseconds, routingKey, priority, cancellationToken);

    public async Task PublishBatch<T>(IEnumerable<T> events, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var config = _eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
        {
            return;
        }
        var list = events.ToList();
        if (list.Count is 0)
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
        if (config.Exchange.Type != EModel.None)
        {
            await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, config.Exchange.Arguments, cancellationToken: cancellationToken);
        }
        var effectiveBatchSize = Math.Min(_options.Get(Constant.OptionName).BatchSize, list.Count);
        foreach (var batch in list.Chunk(effectiveBatchSize))
        {
            await PublishBatchInternal(channel, config, batch, properties, routingKey, cancellationToken);
        }
    }

    public async Task PublishBatch<T>(IEnumerable<T> events, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var config = _eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
        {
            return;
        }
        if (config.Exchange.Type != EModel.Delayed)
        {
            throw new InvalidOperationException($"The exchange type for the delayed queue must be '{nameof(EModel.Delayed)}'. Event: '{events.FirstOrDefault()?.GetType().Name ?? typeof(T).Name}'");
        }
        var list = events.ToList();
        if (list.Count is 0)
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

        // Handle headers with x-delay
        var headers = new Dictionary<string, object?>(config.Headers);
        var xDelay = headers.TryGetValue("x-delay", out var delay);
        headers["x-delay"] = xDelay && ttl is 0 && delay is not null ? delay : ttl;
        properties.Headers = headers;
        var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
        var xDelayedType = exchangeArgs.TryGetValue("x-delayed-type", out var delayedType);
        exchangeArgs["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;
        await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, exchangeArgs, cancellationToken: cancellationToken);
        var batchSize = Math.Min(_options.Get(Constant.OptionName).BatchSize, list.Count);
        foreach (var batch in list.Chunk(batchSize))
        {
            await PublishBatchInternal(channel, config, batch, properties, routingKey, cancellationToken);
        }
    }

    public async Task PublishBatch<T>(IEnumerable<T> events, TimeSpan ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await PublishBatch(events, (uint)ttl.TotalMilliseconds, routingKey, priority, cancellationToken);

    internal async Task RunRabbit() => await InitialRabbit();

    private async Task InitialRabbit()
    {
        var configs = _eventRegistry.GetAllConfigurations();
        foreach (var config in configs)
        {
            if (!config.Enabled)
            {
                continue;
            }
            var eventType = config.EventType;
            if (eventType == typeof(IEvent))
            {
                continue;
            }
            var handlerTypes = config.Handlers.Where(ht => !config.IgnoredHandlers.Contains(ht)).Distinct().ToList();
            if (handlerTypes.Count == 0)
            {
                continue;
            }

            // 根据 HandlerThreadCount 创建多个消费者
            var consumerCount = Math.Max(1, config.HandlerThreadCount);
            for (var i = 0; i < consumerCount; i++)
            {
                var consumerIndex = i; // 捕获循环变量
                await Task.Factory.StartNew(async () =>
                {
                    var ct = _cancellationTokenSource.Token;
                    await using var channel = await CreateConsumerChannel(config, ct);
                    _eventHandlerCache[eventType] = handlerTypes;
                    if (config.Exchange.Type != EModel.None)
                    {
                        await channel.QueueBindAsync(config.Queue.Name, config.Exchange.Name, config.Exchange.RoutingKey, cancellationToken: ct);
                    }
                    await StartBasicConsume(eventType, config, channel, consumerIndex, ct);
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
            _eventHandlerCache.Clear();
            _handleAsyncDelegateCache.Clear();
            await RunRabbit();
        };
        return channel;
    }

    private static async Task DeclareExchangeIfNeeded(EventConfiguration config, IChannel channel, CancellationToken ct)
    {
        if (config.Exchange.Type == EModel.None)
        {
            return;
        }
        var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
        if (config.Exchange.Type == EModel.Delayed)
        {
            exchangeArgs.TryAdd("x-delayed-type", "direct");
        }
        await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, exchangeArgs, cancellationToken: ct);
    }

    private async Task StartBasicConsume(Type eventType, EventConfiguration config, IChannel channel, int consumerIndex, CancellationToken ct)
    {
        if (!_eventHandlerCache.TryGetValue(eventType, out var handlerTypes) || handlerTypes.Count == 0)
        {
            return;
        }
        var rabbitConfig = _options.Get(Constant.OptionName);
        await ConfigureQosIfNeeded(config, channel, rabbitConfig.Qos, ct);
        var consumer = new AsyncEventingBasicConsumer(channel);
        await channel.BasicConsumeAsync(config.Queue.Name, false, consumer, ct);
        consumer.ReceivedAsync += async (_, ea) => await HandleReceivedEvent(eventType, ea, channel, consumerIndex, ct);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Started consumer {ConsumerIndex} for event {EventName}", consumerIndex, eventType.Name);
        }
        while (!channel.IsClosed)
        {
            await Task.Delay(100000, ct);
        }
    }

    private static async Task ConfigureQosIfNeeded(EventConfiguration config, IChannel channel, QosConfig defaultQos, CancellationToken ct)
    {
        var qosToUse = config.Qos.PrefetchCount > 0 ? config.Qos : defaultQos;
        if (qosToUse.PrefetchCount > 0)
        {
            await channel.BasicQosAsync(qosToUse.PrefetchSize, qosToUse.PrefetchCount, qosToUse.Global, ct);
        }
    }

    private async Task HandleReceivedEvent(Type eventType, BasicDeliverEventArgs ea, IChannel channel, int consumerIndex, CancellationToken ct)
    {
        try
        {
            await ProcessEvent(eventType, ea.Body.Span.ToArray(), async () =>
            {
                try
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct).ConfigureAwait(false);
                }
                catch (ObjectDisposedException ex)
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _logger.LogError(ex, "Channel disposed before ACK, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                    }
                }
            }, consumerIndex, ct);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error processing message, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
            }
        }
    }

    private async Task ProcessEvent(Type eventType, byte[] message, Func<ValueTask> ack, int consumerIndex, CancellationToken ct)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Processing event: {EventName} on consumer {ConsumerIndex}", eventType.Name, consumerIndex);
        }
        if (!_eventHandlerCache.TryGetValue(eventType, out var handlerTypes) || handlerTypes.Count == 0)
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
        using var scope = _sp.GetService<IServiceScopeFactory>()?.CreateScope();

        // 每个消息只被一个消费者处理，每个handler执行一次
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
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Consumer {ConsumerIndex} executing handler {HandlerType} for event {EventName}", consumerIndex, handlerType.Name, eventType.Name);
                }
                await pipeline.ExecuteAsync(async _ => await cachedDelegate(@event), ct);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Error executing handler {HandlerType} for event: {EventName}", handlerType.Name, eventType.Name);
                }
                throw;
            }
        }
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
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var eventParam = Expression.Parameter(typeof(object), "event");
        var convertedHandler = Expression.Convert(handlerParam, handler.GetType());
        var convertedEvent = Expression.Convert(eventParam, eventType);
        var methodCall = Expression.Call(convertedHandler, method, convertedEvent);
        var lambda = Expression.Lambda<Func<object, object, Task>>(methodCall, handlerParam, eventParam);
        var compiledDelegate = lambda.Compile();
        return async @event =>
        {
            var task = compiledDelegate(handler, @event) ?? throw new InvalidOperationException($"Handler method '{method.Name}' for event type '{eventType.Name}' returned null Task.");
            await task.ConfigureAwait(false);
        };
    }

    private async Task PublishBatchInternal<T>(IChannel channel, EventConfiguration config, T[] batch, BasicProperties properties, string? routingKey, CancellationToken cancellationToken) where T : IEvent
    {
        var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        // 使用并行发送提高性能
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount / 2, batch.Length), // 限制并行度，避免过载
            CancellationToken = cancellationToken
        };
        await Parallel.ForEachAsync(batch, parallelOptions, async (@event, ct) =>
        {
            var sequenceNumber = await channel.GetNextPublishSequenceNumberAsync(ct);
            var tcs = new TaskCompletionSource<bool>();
            _outstandingConfirms[sequenceNumber] = tcs;
            _outstandingMessages[sequenceNumber] = (@event, routingKey, properties.Priority, 0);
            var body = _serializer.Serialize(@event, @event.GetType());
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Publishing event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
            }
            await pipeline.ExecuteAsync(async innerCt =>
                    await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, innerCt).ConfigureAwait(false),
                ct).ConfigureAwait(false);
        });
    }

    private async Task OnBasicAcks(object sender, BasicAckEventArgs ea) => await CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);

    private async Task OnBasicNacks(object sender, BasicNackEventArgs ea)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("Message nack-ed: DeliveryTag={DeliveryTag}, Multiple={Multiple}", ea.DeliveryTag, ea.Multiple);
        }
        await CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple, true);
    }

    private async Task OnBasicReturn(object sender, BasicReturnEventArgs ea)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("Message returned: ReplyCode={ReplyCode}, ReplyText={ReplyText}", ea.ReplyCode, ea.ReplyText);
        }
        await Task.Yield();
    }

    private async Task CleanOutstandingConfirms(ulong deliveryTag, bool multiple, bool nack = false)
    {
        await _confirmSemaphore.WaitAsync();
        try
        {
            if (multiple)
            {
                var toRemove = _outstandingConfirms.Keys.Where(k => k <= deliveryTag).ToList();
                foreach (var seqNo in toRemove)
                {
                    if (!_outstandingConfirms.TryRemove(seqNo, out var tcs))
                    {
                        continue;
                    }
                    tcs.SetResult(!nack);
                    if (!nack || !_outstandingMessages.TryRemove(seqNo, out var messageInfo))
                    {
                        continue;
                    }
                    var nextRetryTime = DateTime.UtcNow.AddMilliseconds(Math.Min(Math.Pow(2, messageInfo.RetryCount) * 1000, 30000));
                    _nackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
                }
            }
            else if (_outstandingConfirms.TryRemove(deliveryTag, out var tcs))
            {
                tcs.SetResult(!nack);
                if (nack && _outstandingMessages.TryRemove(deliveryTag, out var messageInfo))
                {
                    var nextRetryTime = DateTime.UtcNow.AddMilliseconds(Math.Min(Math.Pow(2, messageInfo.RetryCount) * 1000, 30000));
                    _nackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
                }
            }
        }
        finally
        {
            _confirmSemaphore.Release();
        }
    }

    private async Task StartNackedMessageRetryTask()
    {
        var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                if (_nackedMessages.TryPeek(out var nackedMessage) && nackedMessage.NextRetryTime <= DateTime.UtcNow)
                {
                    if (!_nackedMessages.TryDequeue(out nackedMessage!))
                    {
                        continue;
                    }
                    var rabbitConfig = _options.Get(Constant.OptionName);
                    var maxRetries = rabbitConfig.RetryCount;
                    if (nackedMessage.RetryCount > maxRetries)
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("Message {EventType} with ID {EventId} exceeded maximum retry attempts ({MaxRetries}), giving up", nackedMessage.Event.GetType().Name, nackedMessage.Event.EventId, maxRetries);
                        }
                        continue;
                    }
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Retrying nacked message {EventType} with ID {EventId}, attempt {RetryCount}/{MaxRetries}", nackedMessage.Event.GetType().Name, nackedMessage.Event.EventId, nackedMessage.RetryCount, maxRetries);
                    }
                    try
                    {
                        var eventType = nackedMessage.Event.GetType();
                        var publishDelegate = _publishDelegateCache.GetOrAdd(eventType, CreatePublishDelegate);
                        if (publishDelegate is not null)
                        {
                            await pipeline.ExecuteAsync(async ct => await publishDelegate(this, nackedMessage.Event, nackedMessage.RoutingKey, nackedMessage.Priority, ct), _cancellationTokenSource.Token);
                        }
                        else if (_logger.IsEnabled(LogLevel.Error))
                        {
                            _logger.LogError("Unable to create publish delegate for event type {EventType}", eventType.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                        {
                            _logger.LogError(ex, "Failed to retry message {EventType} with ID {EventId} after retries", nackedMessage.Event.GetType().Name, nackedMessage.Event.EventId);
                        }
                        var backoff = Math.Pow(2, nackedMessage.RetryCount) * 1000;
                        var nextRetryTime = DateTime.UtcNow.AddMilliseconds(Math.Min(backoff, 30000));
                        _nackedMessages.Enqueue((nackedMessage.Event, nackedMessage.RoutingKey, nackedMessage.Priority, nackedMessage.RetryCount + 1, nextRetryTime));
                    }
                }
                else
                {
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Error in nacked message retry task");
                }
            }
        }
    }

    private Func<EventBus, IEvent, string?, byte?, CancellationToken, Task>? CreatePublishDelegate(Type eventType)
    {
        try
        {
            var method = _publishMethodCache.GetOrAdd(eventType, et => typeof(EventBus).GetMethod(nameof(IBus.Publish), [et, typeof(string), typeof(byte?), typeof(CancellationToken)]));
            if (method is null)
            {
                return null;
            }
            var instanceParam = Expression.Parameter(typeof(EventBus), "instance");
            var eventParam = Expression.Parameter(typeof(IEvent), "event");
            var routingKeyParam = Expression.Parameter(typeof(string), "routingKey");
            var priorityParam = Expression.Parameter(typeof(byte?), "priority");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
            var convertedEvent = Expression.Convert(eventParam, eventType);
            var methodCall = Expression.Call(instanceParam, method, convertedEvent, routingKeyParam, priorityParam, cancellationTokenParam);
            var lambda = Expression.Lambda<Func<EventBus, IEvent, string?, byte?, CancellationToken, Task>>(methodCall, instanceParam, eventParam, routingKeyParam, priorityParam, cancellationTokenParam);
            return lambda.Compile();
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to create publish delegate for event type {EventType}", eventType.Name);
            }
            return null;
        }
    }
}
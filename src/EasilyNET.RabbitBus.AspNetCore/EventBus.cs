using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Enums;
using Microsoft.Extensions.Logging;

namespace EasilyNET.RabbitBus.AspNetCore;

internal sealed record EventBus : IBus
{
    private readonly CacheManager _cacheManager;
    private readonly PersistentConnection _conn;
    private readonly ConsumerManager _consumerManager;
    private readonly EventHandlerInvoker _eventHandlerInvoker;
    private readonly EventPublisher _eventPublisher;
    private readonly EventConfigurationRegistry _eventRegistry;
    private readonly ILogger<EventBus> _logger;
    private readonly MessageConfirmManager _messageConfirmManager;

    private CancellationTokenSource _cancellationTokenSource = new();

    public EventBus(PersistentConnection conn, ILogger<EventBus> logger, EventConfigurationRegistry eventRegistry, CacheManager cacheManager, ConsumerManager consumerManager, EventPublisher eventPublisher, EventHandlerInvoker eventHandlerInvoker, MessageConfirmManager messageConfirmManager)
    {
        _conn = conn;
        _logger = logger;
        _eventRegistry = eventRegistry;
        _cacheManager = cacheManager;
        _consumerManager = consumerManager;
        _eventPublisher = eventPublisher;
        _eventHandlerInvoker = eventHandlerInvoker;
        _messageConfirmManager = messageConfirmManager;
        _ = InitializeAsync();
    }

    public async Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var config = _eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
        {
            return;
        }
        await _eventPublisher.Publish(config, @event, routingKey, priority, cancellationToken);
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
        await _eventPublisher.PublishDelayed(config, @event, ttl, routingKey, priority, cancellationToken);
    }

    public async Task Publish<T>(T @event, TimeSpan ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await Publish(@event, (uint)ttl.TotalMilliseconds, routingKey, priority, cancellationToken);

    public async Task PublishBatch<T>(IEnumerable<T> events, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent
    {
        var config = _eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
        {
            return;
        }
        await _eventPublisher.PublishBatch(config, events, routingKey, priority, multiThread, cancellationToken);
    }

    public async Task PublishBatch<T>(IEnumerable<T> events, uint ttl, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent
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
        await _eventPublisher.PublishBatchDelayed(config, events, ttl, routingKey, priority, multiThread, cancellationToken);
    }

    public async Task PublishBatch<T>(IEnumerable<T> events, TimeSpan ttl, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent => await PublishBatch(events, (uint)ttl.TotalMilliseconds, routingKey, priority, multiThread, cancellationToken);

    /// <summary>
    /// 异步初始化EventBus
    /// </summary>
    private async Task InitializeAsync()
    {
        // 注册连接事件
        RegisterConnectionEvents();
        // 注册通道事件
        // 获取通道（可能是异步的）
        var channel = await _conn.GetChannelAsync();
        // 注册异步事件处理器
        channel.BasicAcksAsync += _eventPublisher.OnBasicAcks;
        channel.BasicNacksAsync += _eventPublisher.OnBasicNacks;
        channel.BasicReturnAsync += _eventPublisher.OnBasicReturn;
        // 启动重试任务
        _ = _messageConfirmManager.StartNackedMessageRetryTask(this, _cancellationTokenSource);
        // 初始化RabbitMQ消费者
        await RunRabbit();
    }

    /// <summary>
    /// 注册连接事件
    /// </summary>
    private void RegisterConnectionEvents()
    {
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
            _cacheManager.ClearEventHandlerCaches();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new();
            await RunRabbit();
        };
    }

    internal async Task RunRabbit()
    {
        await _consumerManager.InitializeConsumers((eventType, ea, channel, consumerIndex, ct) =>
            _eventHandlerInvoker.HandleReceivedEvent(eventType, ea, channel, consumerIndex, _cacheManager.EventHandlerCache, ct), _cancellationTokenSource);
    }
}
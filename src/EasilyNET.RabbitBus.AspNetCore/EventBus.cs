using EasilyNET.Core.Misc;
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
    private CancellationTokenSource _cancellationTokenSource = new();

    public EventBus(
        PersistentConnection conn,
        ILogger<EventBus> logger,
        EventConfigurationRegistry eventRegistry,
        CacheManager cacheManager,
        ConsumerManager consumerManager,
        EventPublisher eventPublisher,
        EventHandlerInvoker eventHandlerInvoker)
    {
        _conn = conn;
        _logger = logger;
        _eventRegistry = eventRegistry;
        _cacheManager = cacheManager;
        _consumerManager = consumerManager;
        _eventPublisher = eventPublisher;
        _eventHandlerInvoker = eventHandlerInvoker;
        _ = InitializeAsync(_cancellationTokenSource.Token);
    }

    public async Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await PublishInternal(@event, routingKey, priority, null, cancellationToken).ConfigureAwait(false);

    public async Task Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await PublishInternal(@event, routingKey, priority, ttl, cancellationToken).ConfigureAwait(false);

    public async Task Publish<T>(T @event, TimeSpan ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await Publish(@event, (uint)ttl.TotalMilliseconds, routingKey, priority, cancellationToken);

    public async Task PublishBatch<T>(IEnumerable<T> events, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent => await PublishBatchInternal(events, routingKey, priority, multiThread, null, cancellationToken).ConfigureAwait(false);

    public async Task PublishBatch<T>(IEnumerable<T> events, uint ttl, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent => await PublishBatchInternal(events, routingKey, priority, multiThread, ttl, cancellationToken).ConfigureAwait(false);

    public async Task PublishBatch<T>(IEnumerable<T> events, TimeSpan ttl, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent => await PublishBatch(events, (uint)ttl.TotalMilliseconds, routingKey, priority, multiThread, cancellationToken);

    /// <summary>
    /// 异步初始化EventBus
    /// </summary>
    private async Task InitializeAsync(CancellationToken ct)
    {
        try
        {
            await ValidateExchangesOnStartupAsync(ct).ConfigureAwait(false);
            RegisterConnectionEvents(ct);
            await RegisterChannelEventsAsync(ct).ConfigureAwait(false);
            await RunRabbit().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "EventBus initialization failed; background components will rely on reconnection to recover");
            }
        }
    }

    /// <summary>
    /// 注册连接事件
    /// </summary>
    private void RegisterConnectionEvents(CancellationToken ct)
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

            // 清理过期的发布确认
            await _eventPublisher.OnConnectionReconnected();
            await RegisterChannelEventsAsync(ct).ConfigureAwait(false); // 通道可能已替换，需要重新注册
            await RunRabbit().ConfigureAwait(false);
        };
    }

    private async Task RegisterChannelEventsAsync(CancellationToken ct)
    {
        var channel = await _conn.GetChannelAsync(ct).ConfigureAwait(false);
        // 先移除现有的事件处理器，避免重复注册
        channel.BasicAcksAsync -= _eventPublisher.OnBasicAcks;
        channel.BasicNacksAsync -= _eventPublisher.OnBasicNacks;
        channel.BasicReturnAsync -= _eventPublisher.OnBasicReturn;
        // 然后重新注册
        channel.BasicAcksAsync += _eventPublisher.OnBasicAcks;
        channel.BasicNacksAsync += _eventPublisher.OnBasicNacks;
        channel.BasicReturnAsync += _eventPublisher.OnBasicReturn;
    }

    internal async Task RunRabbit()
    {
        await _consumerManager.InitializeConsumers((eventType, ea, channel, consumerIndex, ct) =>
            _eventHandlerInvoker.HandleReceivedEvent(eventType, ea, channel, consumerIndex, _cacheManager.EventHandlerCache, ct), _cancellationTokenSource);
    }

    /// <summary>
    /// 在启动阶段验证所有配置的交换机
    /// </summary>
    private async Task ValidateExchangesOnStartupAsync(CancellationToken ct)
    {
        // 这里需要访问RabbitConfig来检查ValidateExchangesOnStartup设置
        // 但是EventBus没有直接访问配置的途径，我们可以通过依赖注入或者其他方式获取
        // 暂时先实现基本的校验逻辑，之后可以优化
        var configurations = _eventRegistry.GetAllConfigurations().ToList();
        if (configurations.Count == 0)
        {
            return;
        }
        try
        {
            var channel = await _conn.GetChannelAsync(ct).ConfigureAwait(false);
            var validatedExchanges = new HashSet<string>();
            foreach (var config in configurations.Where(c => c.Exchange.Type != EModel.None))
            {
                var exchangeKey = $"{config.Exchange.Name}:{config.Exchange.Type}";
                if (validatedExchanges.Contains(exchangeKey))
                {
                    continue; // 已经验证过这个交换机
                }
                try
                {
                    // 使用passive模式验证交换机是否存在且类型匹配
                    await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, config.Exchange.Arguments, true, false, CancellationToken.None).ConfigureAwait(false);
                    validatedExchanges.Add(exchangeKey);
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Exchange {ExchangeName} validated successfully with type {Type}", config.Exchange.Name, config.Exchange.Type.Description);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("inequivalent arg 'type'", StringComparison.OrdinalIgnoreCase))
                    {
                        var errorMessage = $"""
                                            Exchange '{config.Exchange.Name}' type mismatch detected during startup validation. 
                                            Expected: {config.Exchange.Type.Description}. 
                                            This will cause connection closures during runtime. 
                                            Please fix the exchange configuration or set SkipExchangeDeclare=true.
                                            """;
                        if (_logger.IsEnabled(LogLevel.Error))
                        {
                            _logger.LogError(ex, "{ErrorMessage}", errorMessage);
                        }

                        // 在启动阶段发现类型不匹配时，抛出异常让应用fail fast
                        throw new InvalidOperationException(errorMessage, ex);
                    }
                    if (ex.Message.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("Exchange {ExchangeName} does not exist. It will be created during first publish operation.", config.Exchange.Name);
                        }
                    }
                    else
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(ex, "Failed to validate exchange {ExchangeName}", config.Exchange.Name);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to validate exchanges on startup");
            }
            // 如果是类型不匹配错误，让它继续抛出；其他错误则记录但不阻止启动
            if (ex.Message.Contains("type mismatch", StringComparison.OrdinalIgnoreCase))
            {
                throw;
            }
        }
    }

    #region Internal Publish Helpers

    private async Task PublishInternal<T>(T @event, string? routingKey, byte? priority, uint? ttl, CancellationToken ct) where T : IEvent
    {
        ct.ThrowIfCancellationRequested();
        var config = _eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
        {
            return;
        }
        try
        {
            if (ttl.HasValue)
            {
                if (config.Exchange.Type != EModel.Delayed)
                {
                    throw new InvalidOperationException($"The exchange type for the delayed queue must be '{nameof(EModel.Delayed)}'. Event: '{@event.GetType().Name}'");
                }
                await _eventPublisher.PublishDelayed(config, @event, ttl.Value, routingKey, priority, ct).ConfigureAwait(false);
            }
            else
            {
                await _eventPublisher.Publish(config, @event, routingKey, priority, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to publish{Kind}event {EventType} ID {EventId}", ttl.HasValue ? " delayed " : " ", @event.GetType().Name, @event.EventId);
            }
            // Swallow: retry subsystem will handle nacks/timeouts
        }
    }

    private async Task PublishBatchInternal<T>(IEnumerable<T> events, string? routingKey, byte? priority, bool? multiThread, uint? ttl, CancellationToken ct) where T : IEvent
    {
        ct.ThrowIfCancellationRequested();
        var list = events as IList<T> ?? [.. events];
        if (list.Count == 0)
        {
            return;
        }
        var config = _eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
        {
            return;
        }
        try
        {
            if (ttl.HasValue)
            {
                if (config.Exchange.Type != EModel.Delayed)
                {
                    throw new InvalidOperationException($"The exchange type for the delayed queue must be '{nameof(EModel.Delayed)}'. Event: '{typeof(T).Name}'");
                }
                await _eventPublisher.PublishBatchDelayed(config, list, ttl.Value, routingKey, priority, multiThread, ct).ConfigureAwait(false);
            }
            else
            {
                await _eventPublisher.PublishBatch(config, list, routingKey, priority, multiThread, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to publish{Kind}batch events {EventType} (Count={Count})", ttl.HasValue ? " delayed " : " ", typeof(T).Name, list.Count);
            }
        }
    }

    #endregion
}
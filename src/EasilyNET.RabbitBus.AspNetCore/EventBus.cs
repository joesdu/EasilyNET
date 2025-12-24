using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasilyNET.RabbitBus.AspNetCore;

internal sealed class EventBus(
    PersistentConnection conn,
    EventConfigurationRegistry eventRegistry,
    ConsumerManager consumerManager,
    EventPublisher eventPublisher,
    EventHandlerInvoker handlerInvoker,
    ILogger<EventBus> logger,
    IOptionsMonitor<RabbitConfig> options) : IBus
{
    private readonly RabbitConfig _config = options.Get(Constant.OptionName);

    public async Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await PublishInternal(@event, routingKey, priority, null, cancellationToken).ConfigureAwait(false);

    public async Task Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await PublishInternal(@event, routingKey, priority, ttl, cancellationToken).ConfigureAwait(false);

    public async Task Publish<T>(T @event, TimeSpan ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await Publish(@event, (uint)ttl.TotalMilliseconds, routingKey, priority, cancellationToken);

    public async Task PublishBatch<T>(IEnumerable<T> events, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await PublishBatchInternal(events, routingKey, priority, null, cancellationToken).ConfigureAwait(false);

    public async Task PublishBatch<T>(IEnumerable<T> events, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await PublishBatchInternal(events, routingKey, priority, ttl, cancellationToken).ConfigureAwait(false);

    public async Task PublishBatch<T>(IEnumerable<T> events, TimeSpan ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent => await PublishBatch(events, (uint)ttl.TotalMilliseconds, routingKey, priority, cancellationToken);

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
            await RestartRabbit(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "EventBus initialization failed; background components will rely on reconnection to recover");
            }
        }
    }

    /// <summary>
    /// 注册连接事件
    /// </summary>
    private void RegisterConnectionEvents(CancellationToken ct)
    {
        conn.ConnectionDisconnected += (_, _) =>
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("RabbitMQ connection disconnected. Stopping consumers...");
            }
        };
        conn.ConnectionReconnected += async (_, _) =>
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("RabbitMQ connection reconnected. Reinitializing consumers...");
            }
            handlerInvoker.ClearEventHandlerCaches();

            // 清理过期的发布确认
            await eventPublisher.OnConnectionReconnected();
            await RegisterChannelEventsAsync(ct).ConfigureAwait(false); // 通道可能已替换，需要重新注册
            await RestartRabbit(ct).ConfigureAwait(false);
        };
    }

    private async Task RegisterChannelEventsAsync(CancellationToken ct)
    {
        var channel = await conn.GetChannelAsync(ct).ConfigureAwait(false);
        // 先移除现有的事件处理器，避免重复注册
        channel.BasicAcksAsync -= eventPublisher.OnBasicAcks;
        channel.BasicNacksAsync -= eventPublisher.OnBasicNacks;
        channel.BasicReturnAsync -= eventPublisher.OnBasicReturn;
        // 然后重新注册
        channel.BasicAcksAsync += eventPublisher.OnBasicAcks;
        channel.BasicNacksAsync += eventPublisher.OnBasicNacks;
        channel.BasicReturnAsync += eventPublisher.OnBasicReturn;
    }

    private async Task RestartRabbit(CancellationToken cancellationToken)
    {
        await consumerManager.InitializeConsumers((eventType, ea, channel, consumerIndex, ct) =>
            handlerInvoker.HandleReceivedEvent(eventType, ea, channel, consumerIndex, handlerInvoker.EventHandlerCache, ct), cancellationToken);
    }

    internal async Task RunRabbit(CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// 在启动阶段验证所有配置的交换机
    /// </summary>
    private async Task ValidateExchangesOnStartupAsync(CancellationToken ct)
    {
        var configurations = eventRegistry.GetAllConfigurations().ToHashSet();
        if (configurations.Count == 0)
        {
            return;
        }
        // 过滤出需要验证的配置
        var configsToValidate = configurations
                                .Where(c => c.Exchange.Type != EModel.None &&
                                            !(c.SkipExchangeDeclare ?? _config.SkipExchangeDeclare) &&
                                            (c.ValidateExchangeOnStartup ?? _config.ValidateExchangesOnStartup))
                                .ToList();
        if (configsToValidate.Count == 0)
        {
            return;
        }
        try
        {
            var channel = await conn.GetChannelAsync(ct).ConfigureAwait(false);
            var validatedExchanges = new HashSet<string>();
            foreach (var config in from config in configsToValidate let exchangeKey = $"{config.Exchange.Name}:{config.Exchange.Type}" where validatedExchanges.Add(exchangeKey) select config)
            {
                try
                {
                    // 使用passive模式验证交换机是否存在且类型匹配
                    await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, config.Exchange.Arguments, true, false, ct).ConfigureAwait(false);
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Exchange {ExchangeName} validated successfully with type {Type}", config.Exchange.Name, config.Exchange.Type.Description);
                    }
                }
                catch (Exception ex)
                {
                    HandleExchangeValidationException(ex, config);
                }
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to validate exchanges on startup");
            }
            // 如果是类型不匹配错误，让它继续抛出；其他错误则记录但不阻止启动
            if (ex.Message.Contains("type mismatch", StringComparison.OrdinalIgnoreCase))
            {
                throw;
            }
        }
    }

    private void HandleExchangeValidationException(Exception ex, EventConfiguration config)
    {
        if (ex.Message.Contains("inequivalent arg 'type'", StringComparison.OrdinalIgnoreCase))
        {
            var errorMessage = $"""
                                Exchange '{config.Exchange.Name}' type mismatch detected during startup validation. 
                                Expected: {config.Exchange.Type.Description}. 
                                This will cause connection closures during runtime. 
                                Please fix the exchange configuration or set SkipExchangeDeclare=true.
                                """;
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "{ErrorMessage}", errorMessage);
            }

            // 在启动阶段发现类型不匹配时，抛出异常让应用fail fast
            throw new InvalidOperationException(errorMessage, ex);
        }
        if (ex.Message.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase))
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Exchange {ExchangeName} does not exist. It will be created during first publish operation.", config.Exchange.Name);
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Failed to validate exchange {ExchangeName}", config.Exchange.Name);
            }
        }
    }

    #region Internal Publish Helpers

    private async Task PublishInternal<T>(T @event, string? routingKey, byte? priority, uint? ttl, CancellationToken ct) where T : IEvent
    {
        ct.ThrowIfCancellationRequested();
        var config = eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Event {EventType} is not registered or disabled.", typeof(T).Name);
            }
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
                await eventPublisher.PublishDelayed(config, @event, ttl.Value, routingKey, priority, ct).ConfigureAwait(false);
            }
            else
            {
                await eventPublisher.Publish(config, @event, routingKey, priority, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to publish{Kind}event {EventType} ID {EventId}", ttl.HasValue ? " delayed " : " ", @event.GetType().Name, @event.EventId);
            }
            // Swallow: retry subsystem will handle nacks/timeouts
        }
    }

    private async Task PublishBatchInternal<T>(IEnumerable<T> events, string? routingKey, byte? priority, uint? ttl, CancellationToken ct) where T : IEvent
    {
        ct.ThrowIfCancellationRequested();
        // 避免多次枚举
        if (events is not ICollection<T> list)
        {
            list = [.. events];
        }
        if (list.Count == 0)
        {
            return;
        }
        var config = eventRegistry.GetConfiguration<T>();
        if (config is null || !config.Enabled)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Event {EventType} is not registered or disabled.", typeof(T).Name);
            }
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
                await eventPublisher.PublishBatchDelayed(config, list, ttl.Value, routingKey, priority, ct).ConfigureAwait(false);
            }
            else
            {
                await eventPublisher.PublishBatch(config, list, routingKey, priority, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to publish{Kind}batch events {EventType} (Count={Count})", ttl.HasValue ? " delayed " : " ", typeof(T).Name, list.Count);
            }
        }
    }

    #endregion

    public async Task Publish(object @event, Type eventType, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (@event is not IEvent evt)
        {
            throw new ArgumentException("Event must implement IEvent", nameof(@event));
        }
        var config = eventRegistry.GetConfiguration(eventType);
        if (config is null || !config.Enabled)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Event {EventType} is not registered or disabled.", eventType.Name);
            }
            return;
        }
        try
        {
            await eventPublisher.Publish(config, evt, routingKey, priority, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to publish event {EventType} ID {EventId}", eventType.Name, evt.EventId);
            }
        }
    }
}
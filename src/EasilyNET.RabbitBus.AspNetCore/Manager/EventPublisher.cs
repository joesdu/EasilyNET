using System.Collections.Concurrent;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Metrics;
using EasilyNET.RabbitBus.AspNetCore.Utilities;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 事件发布器，负责处理所有事件发布相关逻辑
/// </summary>
internal sealed class EventPublisher(PersistentConnection conn, IBusSerializer serializer, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pipelineProvider, IOptionsMonitor<RabbitConfig> options)
{
    private static readonly TimeSpan MinRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);
    private readonly SemaphoreSlim _confirmSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<bool>> _outstandingConfirms = [];
    private readonly ConcurrentDictionary<ulong, (IEvent Event, string? RoutingKey, byte? Priority, int RetryCount)> _outstandingMessages = [];

    public ConcurrentQueue<(IEvent Event, string? RoutingKey, byte? Priority, int RetryCount, DateTime NextRetryTime)> NackedMessages { get; } = [];

    // 计算指数退避时间 2^n * 1s 带上限
    private static TimeSpan CalcBackoff(int retryCount) => BackoffUtility.Exponential(retryCount, MinRetryDelay, MinRetryDelay, MaxRetryDelay);

    private static BasicProperties BuildBasicProperties(EventConfiguration config, byte priority, bool delayed = false, uint? ttl = null)
    {
        var bp = new BasicProperties
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            Priority = priority
        };
        if (!delayed)
        {
            if (config.Headers.Count > 0)
            {
                bp.Headers = new Dictionary<string, object?>(config.Headers);
            }
        }
        else
        {
            var headers = new Dictionary<string, object?>(config.Headers);
            var hasDelay = headers.TryGetValue("x-delay", out var existingDelay);
            headers["x-delay"] = hasDelay && ttl == 0 && existingDelay is not null ? existingDelay : ttl ?? 0u;
            bp.Headers = headers;
        }
        return bp;
    }

    private async Task<(ulong Sequence, TaskCompletionSource<bool> Tcs)> RegisterPendingAsync(IChannel channel, IEvent @event, string? routingKey, byte priority, CancellationToken ct)
    {
        var sequenceNumber = await channel.GetNextPublishSequenceNumberAsync(ct).ConfigureAwait(false);
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _outstandingConfirms[sequenceNumber] = tcs;
        _outstandingMessages[sequenceNumber] = (@event, routingKey, priority, 0);
        RabbitBusMetrics.OutstandingConfirms.Add(1);
        return (sequenceNumber, tcs);
    }

    private async Task ThrottleIfNeededAsync(RabbitConfig cfg, CancellationToken ct)
    {
        if (!cfg.PublisherConfirms)
        {
            return; // 无确认模式无法准确统计，直接返回
        }
        var max = cfg.MaxOutstandingConfirms;
        if (max <= 0)
        {
            return;
        }
        // 简单自旋等待 + 延迟，可改为信号量优化
        var spinWait = 0;
        while (_outstandingConfirms.Count >= max)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(5 + Math.Min(20, spinWait), ct).ConfigureAwait(false);
            spinWait = Math.Min(spinWait + 5, 50);
        }
    }

    private async Task WaitForConfirmIfNeededAsync(TaskCompletionSource<bool> tcs, RabbitConfig cfg, string eventName, string eventId, bool delayed, CancellationToken ct)
    {
        if (!cfg.PublisherConfirms)
        {
            return;
        }
        try
        {
            await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(cfg.ConfirmTimeoutMs), ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Timeout waiting for publisher confirm for {Kind} event: {EventName} with ID: {EventId}", delayed ? "delayed" : "normal", eventName, eventId);
            }
            throw;
        }
    }

    public async Task Publish<T>(EventConfiguration config, T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rabbitConfig = options.Get(Constant.OptionName);
        var channel = await conn.GetChannelAsync().ConfigureAwait(false);
        if (config.Exchange.Type != EModel.None)
        {
            await DeclareExchangeSafelyAsync(channel, config.Exchange.Name, config.Exchange.Type.Description,
                config.Exchange.Durable, config.Exchange.AutoDelete, config.Exchange.Arguments, cancellationToken).ConfigureAwait(false);
        }
        await ThrottleIfNeededAsync(rabbitConfig, cancellationToken).ConfigureAwait(false);
        var properties = BuildBasicProperties(config, priority.GetValueOrDefault());
        var (sequenceNumber, tcs) = await RegisterPendingAsync(channel, @event, routingKey, properties.Priority, cancellationToken).ConfigureAwait(false);
        var body = serializer.Serialize(@event, @event.GetType());
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        try
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Publishing event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
                }
                await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _outstandingConfirms.TryRemove(sequenceNumber, out _);
            _outstandingMessages.TryRemove(sequenceNumber, out _);
            throw;
        }
        try
        {
            await WaitForConfirmIfNeededAsync(tcs, rabbitConfig, @event.GetType().Name, @event.EventId, false, cancellationToken).ConfigureAwait(false);
            RabbitBusMetrics.PublishedNormal.Add(1);
        }
        catch (TimeoutException)
        {
            RabbitBusMetrics.ConfirmTimeout.Add(1);
            throw;
        }
    }

    public async Task PublishDelayed<T>(EventConfiguration config, T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rabbitConfig = options.Get(Constant.OptionName);
        var channel = await conn.GetChannelAsync().ConfigureAwait(false);
        var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
        var hasDelayedType = exchangeArgs.TryGetValue("x-delayed-type", out var delayedType);
        exchangeArgs["x-delayed-type"] = !hasDelayedType || delayedType is null ? "direct" : delayedType;
        await DeclareExchangeSafelyAsync(channel, config.Exchange.Name, config.Exchange.Type.Description,
            config.Exchange.Durable, config.Exchange.AutoDelete, exchangeArgs, cancellationToken).ConfigureAwait(false);
        await ThrottleIfNeededAsync(rabbitConfig, cancellationToken).ConfigureAwait(false);
        var properties = BuildBasicProperties(config, priority.GetValueOrDefault(), true, ttl);
        var (sequenceNumber, tcs) = await RegisterPendingAsync(channel, @event, routingKey, properties.Priority, cancellationToken).ConfigureAwait(false);
        var body = serializer.Serialize(@event, @event.GetType());
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        try
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Publishing delayed event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
                }
                await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _outstandingConfirms.TryRemove(sequenceNumber, out _);
            _outstandingMessages.TryRemove(sequenceNumber, out _);
            throw;
        }
        try
        {
            await WaitForConfirmIfNeededAsync(tcs, rabbitConfig, @event.GetType().Name, @event.EventId, true, cancellationToken).ConfigureAwait(false);
            RabbitBusMetrics.PublishedDelayed.Add(1);
        }
        catch (TimeoutException)
        {
            RabbitBusMetrics.ConfirmTimeout.Add(1);
            throw;
        }
    }

    public async Task PublishBatch<T>(EventConfiguration config, IEnumerable<T> events, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent
    {
        var list = events.ToList();
        if (list.Count is 0)
        {
            return;
        }
        var channel = await conn.GetChannelAsync().ConfigureAwait(false);
        if (config.Exchange.Type != EModel.None)
        {
            await DeclareExchangeSafelyAsync(channel, config.Exchange.Name, config.Exchange.Type.Description,
                config.Exchange.Durable, config.Exchange.AutoDelete, config.Exchange.Arguments, cancellationToken).ConfigureAwait(false);
        }
        var properties = BuildBasicProperties(config, priority.GetValueOrDefault());
        var rabbitCfg = options.Get(Constant.OptionName);
        var effectiveBatchSize = Math.Min(rabbitCfg.BatchSize, list.Count);
        foreach (var batch in list.Chunk(effectiveBatchSize))
        {
            await ThrottleIfNeededAsync(rabbitCfg, cancellationToken).ConfigureAwait(false);
            await PublishBatchInternal(channel, config, batch, properties, routingKey, multiThread, cancellationToken);
        }
    }

    public async Task PublishBatchDelayed<T>(EventConfiguration config, IEnumerable<T> events, uint ttl, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent
    {
        var list = events.ToList();
        if (list.Count is 0)
        {
            return;
        }
        var channel = await conn.GetChannelAsync().ConfigureAwait(false);
        var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
        var xDelayedType = exchangeArgs.TryGetValue("x-delayed-type", out var delayedType);
        exchangeArgs["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;
        await DeclareExchangeSafelyAsync(channel, config.Exchange.Name, config.Exchange.Type.Description,
            config.Exchange.Durable, config.Exchange.AutoDelete, exchangeArgs, cancellationToken).ConfigureAwait(false);
        var properties = BuildBasicProperties(config, priority.GetValueOrDefault(), true, ttl);
        var rabbitCfg = options.Get(Constant.OptionName);
        var batchSize = Math.Min(rabbitCfg.BatchSize, list.Count);
        foreach (var batch in list.Chunk(batchSize))
        {
            await ThrottleIfNeededAsync(rabbitCfg, cancellationToken).ConfigureAwait(false);
            await PublishBatchInternal(channel, config, batch, properties, routingKey, multiThread, cancellationToken);
        }
    }

    private async Task PublishBatchInternal<T>(IChannel channel, EventConfiguration config, T[] batch, BasicProperties properties, string? routingKey, bool? multiThread, CancellationToken cancellationToken) where T : IEvent
    {
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        if (multiThread is true && batch.Length > 1 && logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Batch publish requested multiThread but using sequential publish on single channel for safety. BatchSize={Size}", batch.Length);
        }
        foreach (var @event in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (seq, _) = await RegisterPendingAsync(channel, @event, routingKey, properties.Priority, cancellationToken).ConfigureAwait(false);
            var body = serializer.Serialize(@event, @event.GetType());
            try
            {
                await pipeline.ExecuteAsync(async ct =>
                {
                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace("Publishing event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, seq);
                    }
                    await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _outstandingConfirms.TryRemove(seq, out _);
                _outstandingMessages.TryRemove(seq, out _);
                throw;
            }
            RabbitBusMetrics.PublishedBatch.Add(1);
            // 批量模式下保持与原实现一致：不等待单条 confirm，可按需扩展。
        }
    }

    public async Task OnBasicAcks(object? sender, BasicAckEventArgs ea) => await CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);

    public async Task OnBasicNacks(object? sender, BasicNackEventArgs ea)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("Message nack-ed: DeliveryTag={DeliveryTag}, Multiple={Multiple}", ea.DeliveryTag, ea.Multiple);
        }
        await CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple, true);
    }

    public async Task OnBasicReturn(object? sender, BasicReturnEventArgs ea)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("Message returned: ReplyCode={ReplyCode}, ReplyText={ReplyText}, Exchange={Exchange}, RoutingKey={RoutingKey}", ea.ReplyCode, ea.ReplyText, ea.Exchange, ea.RoutingKey);
        }
        // 无法路由时可考虑重发或其他处理，此处暂保持占位
        await Task.Yield();
    }

    public async Task OnConnectionReconnected()
    {
        // 重连后所有旧确认失效
        await CleanExpiredConfirms();
    }

    private async Task CleanExpiredConfirms()
    {
        await _confirmSemaphore.WaitAsync();
        try
        {
            var expiredConfirms = _outstandingConfirms.ToList();
            foreach (var (sequenceNumber, tcs) in expiredConfirms)
            {
                if (!_outstandingConfirms.TryRemove(sequenceNumber, out _))
                {
                    continue;
                }
                tcs.TrySetResult(false); // 标记失败
                if (!_outstandingMessages.TryRemove(sequenceNumber, out var messageInfo))
                {
                    continue;
                }
                var nextRetryTime = DateTime.UtcNow + MinRetryDelay; // 1 秒后重试
                NackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
                RabbitBusMetrics.RetryEnqueued.Add(1);
                RabbitBusMetrics.OutstandingConfirms.Add(-1);
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Cleaned {Count} expired publisher confirms after reconnection", expiredConfirms.Count);
            }
        }
        finally
        {
            _confirmSemaphore.Release();
        }
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
                    var nextRetryTime = DateTime.UtcNow + CalcBackoff(messageInfo.RetryCount);
                    NackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
                    RabbitBusMetrics.RetryEnqueued.Add(1);
                }
                RabbitBusMetrics.OutstandingConfirms.Add(-toRemove.Count);
                if (nack)
                {
                    RabbitBusMetrics.ConfirmNack.Add(toRemove.Count);
                }
                else
                {
                    RabbitBusMetrics.ConfirmAck.Add(toRemove.Count);
                }
            }
            else if (_outstandingConfirms.TryRemove(deliveryTag, out var tcs))
            {
                tcs.SetResult(!nack);
                if (nack && _outstandingMessages.TryRemove(deliveryTag, out var messageInfo))
                {
                    var nextRetryTime = DateTime.UtcNow + CalcBackoff(messageInfo.RetryCount);
                    NackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
                    RabbitBusMetrics.RetryEnqueued.Add(1);
                }
                RabbitBusMetrics.OutstandingConfirms.Add(-1);
                if (nack)
                {
                    RabbitBusMetrics.ConfirmNack.Add(1);
                }
                else
                {
                    RabbitBusMetrics.ConfirmAck.Add(1);
                }
            }
        }
        finally
        {
            _confirmSemaphore.Release();
        }
    }

    private async Task DeclareExchangeSafelyAsync(IChannel channel, string exchangeName, string exchangeType, bool durable, bool autoDelete, IDictionary<string, object?>? arguments, CancellationToken cancellationToken)
    {
        var rabbitConfig = options.Get(Constant.OptionName);
        if (rabbitConfig.SkipExchangeDeclare)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Skipping exchange declaration for {ExchangeName} as SkipExchangeDeclare is enabled", exchangeName);
            }
            return;
        }
        try
        {
            await channel.ExchangeDeclareAsync(exchangeName, exchangeType, durable, autoDelete, arguments, true, false, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("inequivalent arg 'type'", StringComparison.OrdinalIgnoreCase))
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Exchange {ExchangeName} type mismatch detected. Expected: {ExpectedType}. Consider fixing configuration or setting SkipExchangeDeclare=true", exchangeName, exchangeType);
                }
                throw new InvalidOperationException($"Exchange '{exchangeName}' type mismatch. Expected: {exchangeType}. Please fix the exchange configuration or set SkipExchangeDeclare=true", ex);
            }
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Exchange {ExchangeName} not found or other error, declaring with type {Type}", exchangeName, exchangeType);
            }
            await channel.ExchangeDeclareAsync(exchangeName, exchangeType, durable, autoDelete, arguments, false, false, cancellationToken);
        }
    }
}
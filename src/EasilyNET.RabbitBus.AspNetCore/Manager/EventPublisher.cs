using System.Collections.Concurrent;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Metrics;
using EasilyNET.RabbitBus.AspNetCore.Utilities;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 事件发布器，负责处理所有事件发布相关逻辑
/// </summary>
internal sealed class EventPublisher : IAsyncDisposable
{
    private const int LocalDeclareMaxAttempts = 5;
    private static readonly TimeSpan MinRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LocalDeclareRetryDelay = TimeSpan.FromMilliseconds(200);
    private readonly SemaphoreSlim _confirmSemaphore = new(1, 1);

    private readonly PersistentConnection _conn;
    private readonly ILogger<EventBus> _logger;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<bool>> _outstandingConfirms = [];
    private readonly ConcurrentDictionary<ulong, (IEvent Event, string? RoutingKey, byte? Priority, int RetryCount)> _outstandingMessages = [];
    private readonly ResiliencePipeline _pipeline;
    private readonly RabbitConfig _rabbitConfig;
    private readonly IBusSerializer _serializer;
    private readonly SemaphoreSlim? _throttleSemaphore;

    // 构造函数中初始化信号量
    public EventPublisher(PersistentConnection conn, ResiliencePipelineProvider<string> pipelineProvider, IBusSerializer serializer, ILogger<EventBus> logger, IOptionsMonitor<RabbitConfig> options)
    {
        _conn = conn;
        _serializer = serializer;
        _logger = logger;
        _rabbitConfig = options.Get(Constant.OptionName);
        _pipeline = pipelineProvider.GetPipeline(Constant.PublishPipelineName);
        if (_rabbitConfig is { PublisherConfirms: true, MaxOutstandingConfirms: > 0 })
        {
            _throttleSemaphore = new(_rabbitConfig.MaxOutstandingConfirms, _rabbitConfig.MaxOutstandingConfirms);
        }
    }

    public ConcurrentQueue<(IEvent Event, string? RoutingKey, byte? Priority, int RetryCount, DateTime NextRetryTime)> NackedMessages { get; } = [];

    public async ValueTask DisposeAsync()
    {
        _confirmSemaphore.Dispose();
        _throttleSemaphore?.Dispose();
        await ValueTask.CompletedTask;
    }

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

    private static bool IsTransientChannelError(Exception ex) => ex is ObjectDisposedException || ex is AlreadyClosedException || (ex is OperationInterruptedException oi && (oi.InnerException is EndOfStreamException || oi.Message.Contains("End of stream", StringComparison.OrdinalIgnoreCase)));

    private async Task<IChannel> GetChannelAndEnsureExchangeAsync(EventConfiguration config, IDictionary<string, object?> args, bool passive, CancellationToken ct)
    {
        // 在交换机声明阶段进行本地快速重试，避免因通道热切换导致的 ObjectDisposedException 消耗全局重试次数
        for (var attempt = 1; attempt <= LocalDeclareMaxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var channel = await _conn.GetChannelAsync(ct).ConfigureAwait(false);
            try
            {
                await DeclareExchangeSafelyAsync(channel, config, args, passive, ct).ConfigureAwait(false);
                return channel; // 成功
            }
            catch (Exception ex) when (IsTransientChannelError(ex))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(ex, "Transient channel error while declaring exchange {Exchange}, attempt {Attempt}/{MaxAttempts}", config.Exchange.Name, attempt, LocalDeclareMaxAttempts);
                }
                await Task.Delay(LocalDeclareRetryDelay, ct).ConfigureAwait(false);
            }
        }
        // 多次尝试后仍失败，最后一次再拿到一个通道抛出 Declare 的异常，由调用方处理（入队重试）
        var lastChannel = await _conn.GetChannelAsync(ct).ConfigureAwait(false);
        await DeclareExchangeSafelyAsync(lastChannel, config, args, passive, ct).ConfigureAwait(false);
        return lastChannel;
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

    private async Task ThrottleIfNeededAsync(CancellationToken ct)
    {
        if (_throttleSemaphore is null)
        {
            return;
        }
        await _throttleSemaphore.WaitAsync(ct).ConfigureAwait(false);
    }

    private async Task WaitForConfirmIfNeededAsync(TaskCompletionSource<bool> tcs, RabbitConfig cfg, string eventName, string eventId, bool delayed, CancellationToken ct)
    {
        if (!cfg.PublisherConfirms)
        {
            return;
        }
        try
        {
            var completed = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(cfg.ConfirmTimeoutMs), ct).ConfigureAwait(false);
            if (!completed)
            {
                // This case happens if the TCS was marked as failed from another thread, e.g. due to connection loss.
                throw new TimeoutException($"Publisher confirm was cancelled for {eventName} with ID: {eventId}. This may be due to a connection loss.");
            }
        }
        catch (TimeoutException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Timeout waiting for publisher confirm for {Kind} event: {EventName} with ID: {EventId}", delayed ? "delayed" : "normal", eventName, eventId);
            }
            throw; // Re-throw to be caught by the Publish method.
        }
    }

    public async Task Publish<T>(EventConfiguration config, T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        // 1) 交换机声明
        var args = config.Exchange.Arguments;
        IChannel channel;
        try
        {
            channel = config.Exchange.Type != EModel.None
                          ? await GetChannelAndEnsureExchangeAsync(config, args, false, cancellationToken).ConfigureAwait(false)
                          : await _conn.GetChannelAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "Exchange declare failed for event {EventType} ID {EventId}", @event.GetType().Name, @event.EventId);
            }
            throw new InvalidOperationException($"Failed to declare exchange for event {@event.GetType().Name} with ID {@event.EventId}", ex);
        }
        // 2) 背压
        await ThrottleIfNeededAsync(cancellationToken).ConfigureAwait(false);
        ulong sequenceNumber = 0;
        try
        {
            // 3) 注册发布确认并发布
            var properties = BuildBasicProperties(config, priority.GetValueOrDefault());
            var (seq, tcs) = await RegisterPendingAsync(channel, @event, routingKey, properties.Priority, cancellationToken).ConfigureAwait(false);
            sequenceNumber = seq;
            var body = _serializer.Serialize(@event, @event.GetType());
            await _pipeline.ExecuteAsync(async ct =>
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Publishing event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
                }
                await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
            await WaitForConfirmIfNeededAsync(tcs, _rabbitConfig, @event.GetType().Name, @event.EventId, false, cancellationToken).ConfigureAwait(false);
            RabbitBusMetrics.PublishedNormal.Add(1);
        }
        catch (Exception ex)
        {
            // 任何异常都意味着这个在途消息的生命周期结束了
            if (ex is TimeoutException)
            {
                RabbitBusMetrics.ConfirmTimeout.Add(1);
                HandlePublishTimeout(sequenceNumber);
            }
            else
            {
                HandlePublishFailure(sequenceNumber, ex, @event.GetType().Name, @event.EventId);
            }
            // 向上抛出异常，让调用方知道失败了
            throw;
        }
    }

    public async Task PublishDelayed<T>(EventConfiguration config, T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        // 1) 交换机声明
        var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
        var hasDelayedType = exchangeArgs.TryGetValue("x-delayed-type", out var delayedType);
        exchangeArgs["x-delayed-type"] = !hasDelayedType || delayedType is null ? "direct" : delayedType;
        IChannel channel;
        try
        {
            channel = await GetChannelAndEnsureExchangeAsync(config, exchangeArgs, false, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "Exchange declare failed for delayed event {EventType} ID {EventId}", @event.GetType().Name, @event.EventId);
            }
            throw new InvalidOperationException($"Failed to declare exchange for delayed event {@event.GetType().Name} with ID {@event.EventId}", ex);
        }
        // 2) 背压
        await ThrottleIfNeededAsync(cancellationToken).ConfigureAwait(false);
        ulong sequenceNumber = 0;
        try
        {
            // 3) 发布
            var properties = BuildBasicProperties(config, priority.GetValueOrDefault(), true, ttl);
            var (seq, tcs) = await RegisterPendingAsync(channel, @event, routingKey, properties.Priority, cancellationToken).ConfigureAwait(false);
            sequenceNumber = seq;
            var body = _serializer.Serialize(@event, @event.GetType());
            await _pipeline.ExecuteAsync(async ct =>
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Publishing delayed event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
                }
                await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
            await WaitForConfirmIfNeededAsync(tcs, _rabbitConfig, @event.GetType().Name, @event.EventId, true, cancellationToken).ConfigureAwait(false);
            RabbitBusMetrics.PublishedDelayed.Add(1);
        }
        catch (Exception ex)
        {
            if (ex is TimeoutException)
            {
                RabbitBusMetrics.ConfirmTimeout.Add(1);
                HandlePublishTimeout(sequenceNumber);
            }
            else
            {
                HandlePublishFailure(sequenceNumber, ex, @event.GetType().Name, @event.EventId, true);
            }
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
        // 预先确保交换机
        IChannel channel;
        if (config.Exchange.Type != EModel.None)
        {
            try
            {
                channel = await GetChannelAndEnsureExchangeAsync(config, config.Exchange.Arguments, false, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "Exchange declare failed for batch, failing batch publish");
                }
                throw new InvalidOperationException("Failed to declare exchange for batch publish", ex);
            }
        }
        else
        {
            channel = await _conn.GetChannelAsync(cancellationToken).ConfigureAwait(false);
        }
        var properties = BuildBasicProperties(config, priority.GetValueOrDefault());
        var effectiveBatchSize = Math.Min(_rabbitConfig.BatchSize, list.Count);
        foreach (var batch in list.Chunk(effectiveBatchSize))
        {
            // 为批处理中的每条消息获取信号量
            for (var i = 0; i < batch.Length; i++)
            {
                await ThrottleIfNeededAsync(cancellationToken).ConfigureAwait(false);
            }
            try
            {
                await PublishBatchInternal(channel, config, batch, properties, routingKey, multiThread, cancellationToken);
            }
            catch
            {
                // 如果批量发布内部失败，我们需要释放已获取的信号量
                for (var i = 0; i < batch.Length; i++)
                {
                    _throttleSemaphore?.Release();
                }
                throw;
            }
        }
    }

    public async Task PublishBatchDelayed<T>(EventConfiguration config, IEnumerable<T> events, uint ttl, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent
    {
        var list = events.ToList();
        if (list.Count is 0)
        {
            return;
        }
        var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
        var xDelayedType = exchangeArgs.TryGetValue("x-delayed-type", out var delayedType);
        exchangeArgs["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;
        IChannel channel;
        try
        {
            channel = await GetChannelAndEnsureExchangeAsync(config, exchangeArgs, false, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "Exchange declare failed for delayed batch, failing batch publish");
            }
            throw new InvalidOperationException("Failed to declare exchange for delayed batch publish", ex);
        }
        var properties = BuildBasicProperties(config, priority.GetValueOrDefault(), true, ttl);
        var batchSize = Math.Min(_rabbitConfig.BatchSize, list.Count);
        foreach (var batch in list.Chunk(batchSize))
        {
            for (var i = 0; i < batch.Length; i++)
            {
                await ThrottleIfNeededAsync(cancellationToken).ConfigureAwait(false);
            }
            try
            {
                await PublishBatchInternal(channel, config, batch, properties, routingKey, multiThread, cancellationToken);
            }
            catch
            {
                for (var i = 0; i < batch.Length; i++)
                {
                    _throttleSemaphore?.Release();
                }
                throw;
            }
        }
    }

    private async Task PublishBatchInternal<T>(IChannel channel, EventConfiguration config, T[] batch, BasicProperties properties, string? routingKey, bool? multiThread, CancellationToken cancellationToken) where T : IEvent
    {
        if (multiThread is true && batch.Length > 1 && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Batch publish requested multiThread but using sequential publish on single channel for safety. BatchSize={Size}", batch.Length);
        }
        foreach (var @event in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (seq, _) = await RegisterPendingAsync(channel, @event, routingKey, properties.Priority, cancellationToken).ConfigureAwait(false);
            var body = _serializer.Serialize(@event, @event.GetType());
            try
            {
                await _pipeline.ExecuteAsync(async ct =>
                {
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("Publishing event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, seq);
                    }
                    await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HandlePublishFailure(seq, ex, @event.GetType().Name, @event.EventId);
                // 在批量模式下，一个失败不应阻止其他消息，但需要向上抛出以触发外部的信号量释放
                throw new InvalidOperationException($"Failed to publish event {@event.GetType().Name} with ID {@event.EventId} in batch", ex);
            }
            RabbitBusMetrics.PublishedBatch.Add(1);
            // 批量模式下不等待单条confirm，所以信号量由ACK/NACK回调处理
        }
    }

    public async Task OnBasicAcks(object? _, BasicAckEventArgs ea) => await CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple);

    public async Task OnBasicNacks(object? _, BasicNackEventArgs ea)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("Message nack-ed: DeliveryTag={DeliveryTag}, Multiple={Multiple}", ea.DeliveryTag, ea.Multiple);
        }
        await CleanOutstandingConfirms(ea.DeliveryTag, ea.Multiple, true);
    }

    public async Task OnBasicReturn(object? _, BasicReturnEventArgs ea)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("Message returned: ReplyCode={ReplyCode}, ReplyText={ReplyText}, Exchange={Exchange}, RoutingKey={RoutingKey}", ea.ReplyCode, ea.ReplyText, ea.Exchange, ea.RoutingKey);
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
            var expiredConfirms = _outstandingConfirms.Keys.ToList();
            foreach (var sequenceNumber in expiredConfirms)
            {
                if (!_outstandingConfirms.TryRemove(sequenceNumber, out var tcs))
                {
                    continue;
                }
                tcs.TrySetResult(false); // 标记失败
                if (_outstandingMessages.TryRemove(sequenceNumber, out var messageInfo))
                {
                    var nextRetryTime = DateTime.UtcNow + MinRetryDelay; // 1 秒后重试
                    NackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
                    RabbitBusMetrics.RetryEnqueued.Add(1);
                }
                RabbitBusMetrics.OutstandingConfirms.Add(-1);
                _throttleSemaphore?.Release();
            }
            if (_logger.IsEnabled(LogLevel.Information) && expiredConfirms.Count > 0)
            {
                _logger.LogInformation("Cleaned {Count} expired publisher confirms after reconnection", expiredConfirms.Count);
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
            var toRemove = multiple ? _outstandingConfirms.Keys.Where(k => k <= deliveryTag).ToList() : [deliveryTag];
            foreach (var seqNo in toRemove)
            {
                if (!_outstandingConfirms.TryRemove(seqNo, out var tcs))
                {
                    continue;
                }
                tcs.SetResult(!nack);
                if (nack && _outstandingMessages.TryRemove(seqNo, out var messageInfo))
                {
                    var nextRetryTime = DateTime.UtcNow + CalcBackoff(messageInfo.RetryCount);
                    NackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
                    RabbitBusMetrics.RetryEnqueued.Add(1);
                }
                else if (!nack)
                {
                    // 仅在ACK时移除消息
                    _outstandingMessages.TryRemove(seqNo, out _);
                }
                RabbitBusMetrics.OutstandingConfirms.Add(-1);
                _throttleSemaphore?.Release();
            }
            if (toRemove.Count > 0)
            {
                var metricValue = toRemove.Count;
                if (nack)
                {
                    RabbitBusMetrics.ConfirmNack.Add(metricValue);
                }
                else
                {
                    RabbitBusMetrics.ConfirmAck.Add(metricValue);
                }
            }
        }
        finally
        {
            _confirmSemaphore.Release();
        }
    }

    private bool ShouldSkipExchangeDeclare(EventConfiguration config) => config.SkipExchangeDeclare ?? _rabbitConfig.SkipExchangeDeclare;

    private async Task DeclareExchangeSafelyAsync(IChannel channel, EventConfiguration config, IDictionary<string, object?>? arguments, bool passive, CancellationToken cancellationToken)
    {
        if (ShouldSkipExchangeDeclare(config))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Skipping exchange declaration for {ExchangeName} as SkipExchangeDeclare is enabled", config.Exchange.Name);
            }
            return;
        }
        try
        {
            await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, arguments, passive, false, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("inequivalent arg 'type'", StringComparison.OrdinalIgnoreCase))
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Exchange {ExchangeName} type mismatch detected. Expected: {ExpectedType}. Consider fixing configuration or setting SkipExchangeDeclare=true", config.Exchange.Name, config.Exchange.Type.Description);
                }
                throw new InvalidOperationException($"Exchange '{config.Exchange.Name}' type mismatch. Expected: {config.Exchange.Type.Description}. Please fix the exchange configuration or set SkipExchangeDeclare=true", ex);
            }
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "Exchange {ExchangeName} not found or other error, declaring with type {Type}", config.Exchange.Name, config.Exchange.Type.Description);
            }
            await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, arguments, false, false, cancellationToken);
        }
    }

    private void HandlePublishFailure(ulong sequenceNumber, Exception ex, string eventType, string eventId, bool isDelayed = false)
    {
        if (_outstandingConfirms.TryRemove(sequenceNumber, out _))
        {
            RabbitBusMetrics.OutstandingConfirms.Add(-1);
            _throttleSemaphore?.Release(); // 失败时，必须释放信号量
        }
        _outstandingMessages.TryRemove(sequenceNumber, out _);
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(ex, "Publish failed for {Kind} event {EventType} ID {EventId}", isDelayed ? "delayed" : "normal", eventType, eventId);
        }
    }

    private void HandlePublishTimeout(ulong sequenceNumber)
    {
        if (_outstandingConfirms.TryRemove(sequenceNumber, out var tcs))
        {
            tcs.TrySetResult(false); // Mark as failed
            RabbitBusMetrics.OutstandingConfirms.Add(-1);
            _throttleSemaphore?.Release(); // 超时也必须释放信号量
        }
        if (!_outstandingMessages.TryRemove(sequenceNumber, out var messageInfo))
        {
            return;
        }
        var nextRetryTime = DateTime.UtcNow + CalcBackoff(messageInfo.RetryCount);
        NackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
        RabbitBusMetrics.RetryEnqueued.Add(1);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Event {EventId} enqueued for retry due to publisher confirm timeout.", messageInfo.Event.EventId);
        }
    }
}
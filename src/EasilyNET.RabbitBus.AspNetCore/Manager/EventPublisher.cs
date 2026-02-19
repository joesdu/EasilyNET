using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
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

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 重试消息（使用 struct 减少 GC 压力）
/// </summary>
internal readonly struct RetryMessage
{
    public IEvent Event { get; init; }

    public string? RoutingKey { get; init; }

    public byte? Priority { get; init; }

    public int RetryCount { get; init; }

    public DateTime NextRetryTime { get; init; }
}

/// <summary>
/// 事件发布器，负责处理所有事件发布相关逻辑
/// </summary>
internal sealed class EventPublisher : IAsyncDisposable
{
    /// <summary>
    /// Global <see cref="ActivitySource" /> for RabbitBus event publishing.
    /// This static instance is intentionally process-wide and lives for the entire
    /// application lifetime, so it is not explicitly disposed.
    /// 全局的 <see cref="ActivitySource" /> 用于 RabbitBus 事件发布跟踪。
    /// 该静态实例设计为随应用程序生命周期一直存在，因此不会显式调用 <c>Dispose</c> 进行释放。
    /// </summary>
    private static readonly ActivitySource s_activitySource = new(Constant.ActivitySourceName);

    private static readonly TimeSpan MinRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    // 确认超时截止时间（由后台统一处理超时清理/重试/释放背压）
    private readonly ConcurrentDictionary<ulong, DateTime> _confirmDeadlines = [];
    private readonly SemaphoreSlim _confirmSemaphore = new(1, 1);
    private readonly Task _confirmTimeoutTask;

    private readonly PersistentConnection _conn;
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger<EventBus> _logger;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<bool>> _outstandingConfirms = [];
    private readonly ConcurrentDictionary<ulong, (IEvent Event, string? RoutingKey, byte? Priority, int RetryCount)> _outstandingMessages = [];
    private readonly ResiliencePipeline _pipeline;
    private readonly Task _processQueueTask;
    private readonly Channel<PublishContext> _publishChannel;
    private readonly RabbitConfig _rabbitConfig;

    // 重试 Channel（使用 Channel<T> 替代 ConcurrentQueue）
    private readonly Channel<RetryMessage> _retryChannel;
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
        // 创建无界通道，因为我们使用信号量进行背压控制
        _publishChannel = Channel.CreateUnbounded<PublishContext>(new()
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        // 创建重试通道（单读多写）
        _retryChannel = Channel.CreateUnbounded<RetryMessage>(new()
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _processQueueTask = ProcessQueueAsync();
        _confirmTimeoutTask = MonitorConfirmTimeoutsAsync();
    }

    /// <summary>
    /// 重试消息读取器（用于 MessageConfirmService）
    /// </summary>
    public ChannelReader<RetryMessage> RetryMessageReader => _retryChannel.Reader;

    public async ValueTask DisposeAsync()
    {
        _publishChannel.Writer.TryComplete();
        _retryChannel.Writer.TryComplete();
        await _cts.CancelAsync();
        try
        {
            await _processQueueTask;
            await _confirmTimeoutTask;
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        _cts.Dispose();
        _confirmSemaphore.Dispose();
        _throttleSemaphore?.Dispose();
    }

    /// <summary>
    /// 异步入队重试消息
    /// </summary>
    public ValueTask EnqueueRetryAsync(RetryMessage message, CancellationToken ct = default) => _retryChannel.Writer.WriteAsync(message, ct);

    // 计算指数退避时间 2^n * 1s 带上限
    private static TimeSpan CalcBackoff(int retryCount) => BackoffUtility.Exponential(retryCount, MinRetryDelay, MinRetryDelay, MaxRetryDelay);

    private static BasicProperties BuildBasicProperties(EventConfiguration config, byte priority, Activity? activity = null)
    {
        var bp = new BasicProperties
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            Priority = priority
        };
        if (config.Headers.Count > 0)
        {
            bp.Headers = new Dictionary<string, object?>(config.Headers);
        }
        if (activity is not null)
        {
            bp.Headers ??= new Dictionary<string, object?>();
            if (activity.Id is not null)
            {
                bp.Headers["traceparent"] = activity.Id;
            }
            if (activity.TraceStateString is not null)
            {
                bp.Headers["tracestate"] = activity.TraceStateString;
            }
        }
        return bp;
    }

    private async Task ProcessQueueAsync()
    {
        var reader = _publishChannel.Reader;
        var declaredExchanges = new HashSet<string>();
        IChannel? lastChannel = null;
        var consecutiveErrors = 0; // 连续错误计数
        try
        {
            while (await reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var context))
                {
                    ulong sequenceNumber = 0;
                    var registered = false;
                    try
                    {
                        // 1. 获取通道
                        var channel = await _conn.GetChannelAsync(_cts.Token).ConfigureAwait(false);
                        if (channel != lastChannel)
                        {
                            declaredExchanges.Clear();
                            lastChannel = channel;
                        }

                        // 2. 确保交换机
                        if (context.Config.Exchange.Type != EModel.None && !declaredExchanges.Contains(context.Config.Exchange.Name))
                        {
                            await EnsureExchangeAsync(channel, context.Config, _cts.Token).ConfigureAwait(false);
                            declaredExchanges.Add(context.Config.Exchange.Name);
                        }

                        // 3. 注册发布确认 (信号量已在 Publish 中获取)
                        if (_rabbitConfig.PublisherConfirms)
                        {
                            sequenceNumber = await channel.GetNextPublishSequenceNumberAsync(_cts.Token).ConfigureAwait(false);
                            _outstandingConfirms[sequenceNumber] = context.Tcs;
                            _outstandingMessages[sequenceNumber] = (context.Event, context.RoutingKey, context.Properties.Priority, 0);
                            _confirmDeadlines[sequenceNumber] = DateTime.UtcNow + TimeSpan.FromMilliseconds(_rabbitConfig.ConfirmTimeoutMs);
                            RabbitBusMetrics.OutstandingConfirms.Add(1);
                            registered = true;
                        }

                        // 4. 发布消息
                        var item = context;
                        await _pipeline.ExecuteAsync(async ct =>
                        {
                            if (_logger.IsEnabled(LogLevel.Trace))
                            {
                                _logger.LogTrace("Publishing event: {EventName} with ID: {EventId}, Sequence: {Sequence}", item.Event.GetType().Name, item.Event.EventId, sequenceNumber);
                            }
                            await channel.BasicPublishAsync(item.Config.Exchange.Name, item.RoutingKey ?? item.Config.Exchange.RoutingKey, false, item.Properties, item.Body, ct).ConfigureAwait(false);
                        }, _cts.Token).ConfigureAwait(false);
                        RabbitBusMetrics.PublishedNormal.Add(1);

                        // 成功处理，重置错误计数
                        consecutiveErrors = 0;
                    }
                    catch (Exception ex)
                    {
                        consecutiveErrors++;
                        if (_logger.IsEnabled(LogLevel.Error))
                        {
                            _logger.LogError(ex, "Error processing publish context for event {EventId}", context.Event.EventId);
                        }
                        context.Tcs.TrySetException(ex);
                        if (registered)
                        {
                            // 真正失败：使用标准失败处理
                            HandlePublishFailure(sequenceNumber, ex, context.Event.GetType().Name, context.Event.EventId);
                        }
                        else
                        {
                            // 如果还没注册就失败了（例如 GetChannel 失败），我们需要手动释放信号量
                            _throttleSemaphore?.Release();
                        }

                        // 指数退避，防止 CPU 空转和日志泛滥
                        var delay = BackoffUtility.Exponential(consecutiveErrors, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5));
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("ProcessQueueAsync encountered error. Backing off for {Delay}ms (Attempt {Attempt})", delay.TotalMilliseconds, consecutiveErrors);
                        }
                        try
                        {
                            await Task.Delay(delay, _cts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break; // 退出循环
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex) when (_logger.IsEnabled(LogLevel.Critical))
        {
            _logger.LogCritical(ex, "EventPublisher background task failed unexpectedly");
        }
    }

    private async Task EnsureExchangeAsync(IChannel channel, EventConfiguration config, CancellationToken ct)
    {
        // 创建参数字典的副本，避免修改共享配置对象
        var args = config.Exchange.Arguments.Count > 0
                       ? new Dictionary<string, object?>(config.Exchange.Arguments)
                       : [];
        await DeclareExchangeSafelyAsync(channel, config, args, false, ct).ConfigureAwait(false);
    }

    private async Task ThrottleIfNeededAsync(CancellationToken ct)
    {
        if (_throttleSemaphore is null)
        {
            return;
        }
        await _throttleSemaphore.WaitAsync(ct).ConfigureAwait(false);
    }

    private async Task MonitorConfirmTimeoutsAsync()
    {
        // 统一由后台处理确认超时：清理 outstanding、入重试队列、更新指标、释放背压。
        // 避免在发布线程超时释放导致的 double-release / SemaphoreFullException。
        var tick = TimeSpan.FromMilliseconds(200);
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(tick, _cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                {
                    break;
                }
                if (_confirmDeadlines.IsEmpty)
                {
                    continue;
                }
                var now = DateTime.UtcNow;
                foreach (var (seqNo, deadline) in _confirmDeadlines)
                {
                    if (deadline > now)
                    {
                        continue;
                    }
                    await HandleConfirmTimeoutAsync(seqNo, now).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex) when (_logger.IsEnabled(LogLevel.Critical))
        {
            _logger.LogCritical(ex, "EventPublisher confirm-timeout monitor failed unexpectedly");
        }
    }

    private async Task HandleConfirmTimeoutAsync(ulong seqNo, DateTime now)
    {
        // 与 ack/nack/reconnect 清理保持一致：统一在 _confirmSemaphore 下做原子移除与释放。
        await _confirmSemaphore.WaitAsync(_cts.Token).ConfigureAwait(false);
        try
        {
            if (!_confirmDeadlines.TryGetValue(seqNo, out var deadline) || deadline > now)
            {
                return;
            }
            // 先移除 deadline，避免同一 seqNo 重复处理
            _confirmDeadlines.TryRemove(seqNo, out _);
            if (!_outstandingConfirms.TryRemove(seqNo, out var tcs))
            {
                // 可能已被 ack/nack/reconnect 清理
                _outstandingMessages.TryRemove(seqNo, out _);
                return;
            }

            // 超时：将消息重新入队（若仍可获取到消息信息）。
            // 若重试队列已完成/不可写，TryWrite 会返回 false，此处无需额外处理。
            if (_outstandingMessages.TryRemove(seqNo, out var messageInfo))
            {
                var nextRetryTime = now + CalcBackoff(messageInfo.RetryCount);
                _ = _retryChannel.Writer.TryWrite(new()
                {
                    Event = messageInfo.Event,
                    RoutingKey = messageInfo.RoutingKey,
                    Priority = messageInfo.Priority,
                    RetryCount = messageInfo.RetryCount + 1,
                    NextRetryTime = nextRetryTime
                });
                RabbitBusMetrics.RetryEnqueued.Add(1);
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Publisher confirm timeout. Re-enqueued message for retry: {EventType} ID {EventId}, Sequence {Seq}, RetryCount {Retry}",
                        messageInfo.Event.GetType().Name, messageInfo.Event.EventId, seqNo, messageInfo.RetryCount + 1);
                }
            }

            // 对等待方抛出超时异常（保持原有语义：超时=异常）
            tcs.TrySetException(new TimeoutException($"Publisher confirm timed out. Sequence={seqNo}. Message will be retried."));
            RabbitBusMetrics.OutstandingConfirms.Add(-1);
            _throttleSemaphore?.Release();
        }
        finally
        {
            _confirmSemaphore.Release();
        }
    }

    public async Task Publish<T>(EventConfiguration config, T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        // ReSharper disable once ExplicitCallerInfoArgument
        using var activity = s_activitySource.StartActivity("rabbitmq.publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", config.Exchange.Name);
        activity?.SetTag("messaging.destination_kind", "exchange");
        activity?.SetTag("messaging.rabbitmq.routing_key", routingKey ?? config.Exchange.RoutingKey);
        activity?.SetTag("messaging.message.id", @event.EventId);

        // 先获取信号量
        await ThrottleIfNeededAsync(cancellationToken).ConfigureAwait(false);
        var written = false;
        try
        {
            var body = _serializer.Serialize(@event, @event.GetType());
            var properties = BuildBasicProperties(config, priority.GetValueOrDefault(), activity);
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var context = new PublishContext(@event, routingKey, properties, body, tcs, config);
            await _publishChannel.Writer.WriteAsync(context, cancellationToken).ConfigureAwait(false);
            written = true;
            if (_rabbitConfig.PublisherConfirms)
            {
                _ = await tcs.Task.ConfigureAwait(false);
            }
        }
        catch
        {
            // 仅在未成功写入通道时释放：
            // - 若已写入，背压释放由 ACK/NACK/timeout/reconnect/发布失败统一处理，避免 double-release。
            if (!written)
            {
                _throttleSemaphore?.Release();
            }
            throw;
        }
    }

    public async Task PublishBatch<T>(EventConfiguration config, IEnumerable<T> events, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var list = events.ToList();
        if (list.Count is 0)
        {
            return;
        }
        // ReSharper disable once ExplicitCallerInfoArgument
        using var activity = s_activitySource.StartActivity("rabbitmq.publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", config.Exchange.Name);
        activity?.SetTag("messaging.destination_kind", "exchange");
        activity?.SetTag("messaging.rabbitmq.routing_key", routingKey ?? config.Exchange.RoutingKey);
        var properties = BuildBasicProperties(config, priority.GetValueOrDefault(), activity);
        var tcsList = new List<(TaskCompletionSource<bool> Tcs, string EventId)>(list.Count);
        foreach (var @event in list)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ThrottleIfNeededAsync(cancellationToken).ConfigureAwait(false);
            var written = false;
            try
            {
                var body = _serializer.Serialize(@event, @event.GetType());
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var context = new PublishContext(@event, routingKey, properties, body, tcs, config);
                await _publishChannel.Writer.WriteAsync(context, cancellationToken).ConfigureAwait(false);
                written = true;
                if (_rabbitConfig.PublisherConfirms)
                {
                    tcsList.Add((tcs, @event.EventId));
                }
            }
            finally
            {
                if (!written)
                {
                    _throttleSemaphore?.Release();
                }
            }
        }
        if (_rabbitConfig.PublisherConfirms && tcsList.Count > 0)
        {
            await WaitForBatchConfirmsAsync(tcsList).ConfigureAwait(false);
        }
    }

    private async Task WaitForBatchConfirmsAsync(List<(TaskCompletionSource<bool> Tcs, string EventId)> tcsList)
    {
        try
        {
            var tasks = tcsList.Select(x => x.Tcs.Task).ToList();
            // 等待所有任务完成。确认超时由后台 MonitorConfirmTimeoutsAsync 负责：
            // - 超时会对对应 TCS 设置 TimeoutException，使这里解除阻塞并抛出
            // - 且会原子清理 outstanding 并释放背压，避免 double-release
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            if (results.Any(r => !r))
            {
                throw new IOException("One or more messages in the batch were nacked by the broker.");
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error waiting for batch publisher confirms");
            }
            throw;
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
                _confirmDeadlines.TryRemove(sequenceNumber, out _);
                tcs.TrySetResult(false); // 标记失败
                if (_outstandingMessages.TryRemove(sequenceNumber, out var messageInfo))
                {
                    var nextRetryTime = DateTime.UtcNow + MinRetryDelay;
                    // 使用 Channel 入队
                    _ = _retryChannel.Writer.TryWrite(new()
                    {
                        Event = messageInfo.Event,
                        RoutingKey = messageInfo.RoutingKey,
                        Priority = messageInfo.Priority,
                        RetryCount = messageInfo.RetryCount + 1,
                        NextRetryTime = nextRetryTime
                    });
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
                _confirmDeadlines.TryRemove(seqNo, out _);

                // 正常确认（ACK/NACK）
                tcs.SetResult(!nack);
                switch (nack)
                {
                    case true when _outstandingMessages.TryRemove(seqNo, out var messageInfo):
                    {
                        var nextRetryTime = DateTime.UtcNow + CalcBackoff(messageInfo.RetryCount);
                        _ = _retryChannel.Writer.TryWrite(new()
                        {
                            Event = messageInfo.Event,
                            RoutingKey = messageInfo.RoutingKey,
                            Priority = messageInfo.Priority,
                            RetryCount = messageInfo.RetryCount + 1,
                            NextRetryTime = nextRetryTime
                        });
                        RabbitBusMetrics.RetryEnqueued.Add(1);
                        break;
                    }
                    case false:
                        // 仅在ACK时移除消息
                        _outstandingMessages.TryRemove(seqNo, out _);
                        break;
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
        ArgumentNullException.ThrowIfNull(config);
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

    private void HandlePublishFailure(ulong sequenceNumber, Exception ex, string eventType, string eventId)
    {
        if (_outstandingConfirms.TryRemove(sequenceNumber, out _))
        {
            _confirmDeadlines.TryRemove(sequenceNumber, out _);
            RabbitBusMetrics.OutstandingConfirms.Add(-1);
            _throttleSemaphore?.Release();
        }
        _outstandingMessages.TryRemove(sequenceNumber, out _);
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(ex, "Publish failed for event {EventType} ID {EventId}", eventType, eventId);
        }
    }

    // Confirm timeout is handled by MonitorConfirmTimeoutsAsync

    private record PublishContext(IEvent Event, string? RoutingKey, BasicProperties Properties, ReadOnlyMemory<byte> Body, TaskCompletionSource<bool> Tcs, EventConfiguration Config);
}
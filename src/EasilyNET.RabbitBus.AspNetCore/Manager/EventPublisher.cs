using System.Collections.Concurrent;
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
/// 事件发布器，负责处理所有事件发布相关逻辑
/// </summary>
internal sealed class EventPublisher : IAsyncDisposable
{
    private static readonly TimeSpan MinRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);
    private readonly SemaphoreSlim _confirmSemaphore = new(1, 1);

    private readonly PersistentConnection _conn;
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger<EventBus> _logger;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<bool>> _outstandingConfirms = [];
    private readonly ConcurrentDictionary<ulong, (IEvent Event, string? RoutingKey, byte? Priority, int RetryCount)> _outstandingMessages = [];
    private readonly ResiliencePipeline _pipeline;
    private readonly Task _processQueueTask;
    private readonly Channel<PublishContext> _publishChannel;
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
        // 创建无界通道，因为我们使用信号量进行背压控制
        _publishChannel = Channel.CreateUnbounded<PublishContext>(new()
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _processQueueTask = ProcessQueueAsync();
    }

    public ConcurrentQueue<(IEvent Event, string? RoutingKey, byte? Priority, int RetryCount, DateTime NextRetryTime)> NackedMessages { get; } = [];

    public async ValueTask DisposeAsync()
    {
        _publishChannel.Writer.TryComplete();
        await _cts.CancelAsync();
        try
        {
            await _processQueueTask;
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        _cts.Dispose();
        _confirmSemaphore.Dispose();
        _throttleSemaphore?.Dispose();
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

    private async Task ProcessQueueAsync()
    {
        var reader = _publishChannel.Reader;
        var declaredExchanges = new HashSet<string>();
        IChannel? lastChannel = null;
        var consecutiveErrors = 0; // 连续错误计数
        try
        {
            while (await reader.WaitToReadAsync(_cts.Token))
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
                        sequenceNumber = await channel.GetNextPublishSequenceNumberAsync(_cts.Token).ConfigureAwait(false);
                        _outstandingConfirms[sequenceNumber] = context.Tcs;
                        _outstandingMessages[sequenceNumber] = (context.Event, context.RoutingKey, context.Properties.Priority, 0);
                        RabbitBusMetrics.OutstandingConfirms.Add(1);
                        registered = true;

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
                        if (context.IsDelayed)
                        {
                            RabbitBusMetrics.PublishedDelayed.Add(1);
                        }
                        else
                        {
                            RabbitBusMetrics.PublishedNormal.Add(1);
                        }

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
                            // 如果已经注册，使用标准失败处理（它会释放信号量）
                            HandlePublishFailure(sequenceNumber, ex, context.Event.GetType().Name, context.Event.EventId, context.IsDelayed);
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
        // 对于延迟队列，需要特殊处理参数
        var hasDelayedType = args.TryGetValue("x-delayed-type", out var delayedType);
        args["x-delayed-type"] = !hasDelayedType || delayedType is null ? "direct" : delayedType;
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
                throw new TimeoutException($"Publisher confirm was cancelled for {eventName} with ID: {eventId}. This may be due to a connection loss.");
            }
        }
        catch (TimeoutException)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Timeout waiting for publisher confirm for {Kind} event: {EventName} with ID: {EventId}", delayed ? "delayed" : "normal", eventName, eventId);
            }
            throw;
        }
    }

    public async Task Publish<T>(EventConfiguration config, T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 先获取信号量
        await ThrottleIfNeededAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var body = _serializer.Serialize(@event, @event.GetType());
            var properties = BuildBasicProperties(config, priority.GetValueOrDefault());
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var context = new PublishContext(@event, routingKey, properties, body, tcs, false, config);
            await _publishChannel.Writer.WriteAsync(context, cancellationToken).ConfigureAwait(false);
            await WaitForConfirmIfNeededAsync(tcs, _rabbitConfig, @event.GetType().Name, @event.EventId, false, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // 如果写入通道失败或序列化失败，释放信号量
            _throttleSemaphore?.Release();
            throw;
        }
    }

    public async Task PublishDelayed<T>(EventConfiguration config, T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ThrottleIfNeededAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var body = _serializer.Serialize(@event, @event.GetType());
            var properties = BuildBasicProperties(config, priority.GetValueOrDefault(), true, ttl);
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var context = new PublishContext(@event, routingKey, properties, body, tcs, true, config);
            await _publishChannel.Writer.WriteAsync(context, cancellationToken).ConfigureAwait(false);
            await WaitForConfirmIfNeededAsync(tcs, _rabbitConfig, @event.GetType().Name, @event.EventId, true, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _throttleSemaphore?.Release();
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
        var properties = BuildBasicProperties(config, priority.GetValueOrDefault());
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
                var context = new PublishContext(@event, routingKey, properties, body, tcs, false, config);
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
            await WaitForBatchConfirmsAsync(tcsList, false, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task PublishBatchDelayed<T>(EventConfiguration config, IEnumerable<T> events, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var list = events.ToList();
        if (list.Count is 0)
        {
            return;
        }
        var properties = BuildBasicProperties(config, priority.GetValueOrDefault(), true, ttl);
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
                var context = new PublishContext(@event, routingKey, properties, body, tcs, true, config);
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
            await WaitForBatchConfirmsAsync(tcsList, true, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WaitForBatchConfirmsAsync(List<(TaskCompletionSource<bool> Tcs, string EventId)> tcsList, bool delayed, CancellationToken ct)
    {
        try
        {
            var tasks = tcsList.Select(x => x.Tcs.Task).ToList();
            // 等待所有任务完成，或者超时
            var allTask = Task.WhenAll(tasks);
            var completedTask = await Task.WhenAny(allTask, Task.Delay(TimeSpan.FromMilliseconds(_rabbitConfig.ConfirmTimeoutMs), ct)).ConfigureAwait(false);
            if (completedTask != allTask)
            {
                // 超时处理
                // 找出未完成的任务并记录日志
                var pendingCount = tcsList.Count(x => !x.Tcs.Task.IsCompleted);
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Timeout waiting for batch publisher confirms. {PendingCount}/{TotalCount} messages pending. Kind: {Kind}", pendingCount, tcsList.Count, delayed ? "delayed" : "normal");
                }
                throw new TimeoutException($"Batch publisher confirm timed out. {pendingCount} messages unconfirmed.");
            }

            // 检查是否有被取消/失败的任务 (Task.WhenAll 如果有异常会抛出，但这里我们检查结果)
            // 注意：Tcs.Task 返回 bool，如果 SetResult(false) 表示 Nack
            var results = await allTask.ConfigureAwait(false); // 这里应该已经完成了
            if (results.Any(r => !r))
            {
                throw new IOException("One or more messages in the batch were nacked by the broker.");
            }
        }
        catch (TimeoutException)
        {
            throw;
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
                tcs.TrySetResult(false); // 标记失败
                if (_outstandingMessages.TryRemove(sequenceNumber, out var messageInfo))
                {
                    var nextRetryTime = DateTime.UtcNow + MinRetryDelay;
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
                switch (nack)
                {
                    case true when _outstandingMessages.TryRemove(seqNo, out var messageInfo):
                    {
                        var nextRetryTime = DateTime.UtcNow + CalcBackoff(messageInfo.RetryCount);
                        NackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
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

    private void HandlePublishFailure(ulong sequenceNumber, Exception ex, string eventType, string eventId, bool isDelayed = false)
    {
        if (_outstandingConfirms.TryRemove(sequenceNumber, out _))
        {
            RabbitBusMetrics.OutstandingConfirms.Add(-1);
            _throttleSemaphore?.Release();
        }
        _outstandingMessages.TryRemove(sequenceNumber, out _);
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(ex, "Publish failed for {Kind} event {EventType} ID {EventId}", isDelayed ? "delayed" : "normal", eventType, eventId);
        }
    }

    private record PublishContext(IEvent Event, string? RoutingKey, BasicProperties Properties, ReadOnlyMemory<byte> Body, TaskCompletionSource<bool> Tcs, bool IsDelayed, EventConfiguration Config);
}
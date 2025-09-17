using System.Collections.Concurrent;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Configs;
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
    private readonly SemaphoreSlim _confirmSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<bool>> _outstandingConfirms = [];
    private readonly ConcurrentDictionary<ulong, (IEvent Event, string? RoutingKey, byte? Priority, int RetryCount)> _outstandingMessages = [];

    public ConcurrentQueue<(IEvent Event, string? RoutingKey, byte? Priority, int RetryCount, DateTime NextRetryTime)> NackedMessages { get; } = [];

    public async Task Publish<T>(EventConfiguration config, T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var channel = await conn.GetChannelAsync();
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
        var body = serializer.Serialize(@event, @event.GetType());
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Publishing event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
            }
            await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        var rabbitConfig = options.Get(Constant.OptionName);
        if (rabbitConfig.PublisherConfirms)
        {
            try
            {
                await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(rabbitConfig.ConfirmTimeoutMs), cancellationToken);
            }
            catch (TimeoutException)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Timeout waiting for publisher confirm for event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
                }
                throw;
            }
        }
    }

    public async Task PublishDelayed<T>(EventConfiguration config, T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent
    {
        var channel = await conn.GetChannelAsync();
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
        var body = serializer.Serialize(@event, @event.GetType());
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        await pipeline.ExecuteAsync(async ct =>
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Publishing delayed event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
            }
            await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        var rabbitConfig = options.Get(Constant.OptionName);
        if (rabbitConfig.PublisherConfirms)
        {
            try
            {
                await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(rabbitConfig.ConfirmTimeoutMs), cancellationToken);
            }
            catch (TimeoutException)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Timeout waiting for publisher confirm for delayed event: {EventName} with ID: {EventId}", @event.GetType().Name, @event.EventId);
                }
                throw;
            }
        }
    }

    public async Task PublishBatch<T>(EventConfiguration config, IEnumerable<T> events, string? routingKey = null, byte? priority = 0, bool? multiThread = true, CancellationToken cancellationToken = default) where T : IEvent
    {
        var list = events.ToList();
        if (list.Count is 0)
        {
            return;
        }
        var channel = await conn.GetChannelAsync();
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
        var effectiveBatchSize = Math.Min(options.Get(Constant.OptionName).BatchSize, list.Count);
        foreach (var batch in list.Chunk(effectiveBatchSize))
        {
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
        var channel = await conn.GetChannelAsync();
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
        var batchSize = Math.Min(options.Get(Constant.OptionName).BatchSize, list.Count);
        foreach (var batch in list.Chunk(batchSize))
        {
            await PublishBatchInternal(channel, config, batch, properties, routingKey, multiThread, cancellationToken);
        }
    }

    private async Task PublishBatchInternal<T>(IChannel channel, EventConfiguration config, T[] batch, BasicProperties properties, string? routingKey, bool? multiThread, CancellationToken cancellationToken) where T : IEvent
    {
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        // 使用并行发送提高性能
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount / 2, batch.Length), // 限制并行度，避免过载
            CancellationToken = cancellationToken
        };
        if (multiThread is true)
        {
            await Parallel.ForEachAsync(batch, parallelOptions, async (@event, ct) => await BasicPublish(@event, ct).ConfigureAwait(false));
        }
        else
        {
            foreach (var @event in batch)
            {
                await BasicPublish(@event, cancellationToken).ConfigureAwait(false);
            }
        }
        return;

        async Task BasicPublish(IEvent @event, CancellationToken ct)
        {
            var sequenceNumber = await channel.GetNextPublishSequenceNumberAsync(ct);
            var tcs = new TaskCompletionSource<bool>();
            _outstandingConfirms[sequenceNumber] = tcs;
            _outstandingMessages[sequenceNumber] = (@event, routingKey, properties.Priority, 0);
            var body = serializer.Serialize(@event, @event.GetType());
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Publishing event: {EventName} with ID: {EventId}, Sequence: {Sequence}", @event.GetType().Name, @event.EventId, sequenceNumber);
            }
            await pipeline.ExecuteAsync(async innerCt =>
                    await channel.BasicPublishAsync(config.Exchange.Name, routingKey ?? config.Exchange.RoutingKey, false, properties, body, innerCt).ConfigureAwait(false),
                ct).ConfigureAwait(false);
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
            logger.LogWarning("Message returned: ReplyCode={ReplyCode}, ReplyText={ReplyText}", ea.ReplyCode, ea.ReplyText);
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
                    NackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
                }
            }
            else if (_outstandingConfirms.TryRemove(deliveryTag, out var tcs))
            {
                tcs.SetResult(!nack);
                if (nack && _outstandingMessages.TryRemove(deliveryTag, out var messageInfo))
                {
                    var nextRetryTime = DateTime.UtcNow.AddMilliseconds(Math.Min(Math.Pow(2, messageInfo.RetryCount) * 1000, 30000));
                    NackedMessages.Enqueue((messageInfo.Event, messageInfo.RoutingKey, messageInfo.Priority, messageInfo.RetryCount + 1, nextRetryTime));
                }
            }
        }
        finally
        {
            _confirmSemaphore.Release();
        }
    }
}
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EasilyNET.RabbitBus.AspNetCore.Abstractions;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Metrics;
using EasilyNET.RabbitBus.AspNetCore.Utilities;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 消息确认管理器，负责消息确认和重试逻辑
/// </summary>
internal sealed class MessageConfirmManager(
    EventPublisher eventPublisher,
    IBus iBus,
    ILogger<MessageConfirmManager> logger,
    ResiliencePipelineProvider<string> pipelineProvider,
    IOptionsMonitor<RabbitConfig> options,
    IDeadLetterStore deadLetterStore) : BackgroundService
{
    private readonly ConcurrentDictionary<Type, Func<IBus, IEvent, string?, byte?, CancellationToken, Task>?> _publishDelegateCache = [];
    private readonly ConcurrentDictionary<Type, MethodInfo?> _publishMethodCache = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        const int maxQueueSize = 10000;
        const int batchSize = 20;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 队列长度保护
                if (eventPublisher.NackedMessages.Count > maxQueueSize)
                {
                    const int dropTarget = maxQueueSize / 2;
                    var dropped = 0;
                    while (eventPublisher.NackedMessages.Count > dropTarget && eventPublisher.NackedMessages.TryDequeue(out _))
                    {
                        dropped++;
                    }
                    if (dropped > 0)
                    {
                        RabbitBusMetrics.RetryDiscarded.Add(dropped);
                        if (logger.IsEnabled(LogLevel.Error))
                        {
                            logger.LogError("Dropped {Count} nacked messages due to queue overflow", dropped);
                        }
                    }
                    await Task.Delay(3000, stoppingToken);
                    continue;
                }

                // 收集可重试消息
                var now = DateTime.UtcNow;
                var toRetry = new List<(IEvent Event, string? RoutingKey, byte? Priority, int RetryCount, DateTime NextRetryTime)>();
                while (toRetry.Count < batchSize && eventPublisher.NackedMessages.TryPeek(out var head))
                {
                    if (head.NextRetryTime <= now)
                    {
                        if (eventPublisher.NackedMessages.TryDequeue(out head))
                        {
                            toRetry.Add(head);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                RabbitBusMetrics.SetRetryQueueDepth(eventPublisher.NackedMessages.Count);
                if (toRetry.Count == 0)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }
                foreach (var item in toRetry)
                {
                    var rabbitCfg = options.Get(Constant.OptionName);
                    var maxRetries = rabbitCfg.RetryCount;
                    if (item.RetryCount > maxRetries)
                    {
                        RabbitBusMetrics.RetryDiscarded.Add(1);
                        RabbitBusMetrics.DeadLettered.Add(1);
                        try
                        {
                            await deadLetterStore.StoreAsync(new DeadLetterMessage(item.Event.GetType().Name, item.Event.EventId, DateTime.UtcNow, item.RetryCount, item.Event), stoppingToken);
                        }
                        catch (Exception dlEx)
                        {
                            if (logger.IsEnabled(LogLevel.Error))
                            {
                                logger.LogError(dlEx, "Failed to store dead-letter message {EventType} ID {EventId}", item.Event.GetType().Name, item.Event.EventId);
                            }
                        }
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning("Discarding message {EventType} ID {EventId} after {RetryCount} retries (limit {Limit})", item.Event.GetType().Name, item.Event.EventId, item.RetryCount, maxRetries);
                        }
                        continue;
                    }
                    try
                    {
                        var eventType = item.Event.GetType();
                        var del = _publishDelegateCache.GetOrAdd(eventType, CreatePublishDelegate);
                        if (del is null)
                        {
                            RabbitBusMetrics.RetryRescheduled.Add(1);
                            Reschedule(item);
                            continue;
                        }
                        await pipeline.ExecuteAsync(async ct => await del(iBus, item.Event, item.RoutingKey, item.Priority, ct), stoppingToken);
                        RabbitBusMetrics.RetryAttempt.Add(1);
                    }
                    catch (Exception ex)
                    {
                        if (logger.IsEnabled(LogLevel.Error))
                        {
                            logger.LogError(ex, "Retry failed for {EventType} ID {EventId} attempt {Attempt}", item.Event.GetType().Name, item.Event.EventId, item.RetryCount);
                        }
                        RabbitBusMetrics.RetryRescheduled.Add(1);
                        Reschedule(item);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Unexpected error in retry background loop");
                }
                await Task.Delay(2000, stoppingToken);
            }
        }
        return;

        void Reschedule((IEvent Event, string? RoutingKey, byte? Priority, int RetryCount, DateTime NextRetryTime) msg)
        {
            var next = DateTime.UtcNow + BackoffUtility.Exponential(msg.RetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
            eventPublisher.NackedMessages.Enqueue((msg.Event, msg.RoutingKey, msg.Priority, msg.RetryCount + 1, next));
            RabbitBusMetrics.RetryEnqueued.Add(1);
        }
    }

    private Func<IBus, IEvent, string?, byte?, CancellationToken, Task>? CreatePublishDelegate(Type eventType)
    {
        try
        {
            var method = _publishMethodCache.GetOrAdd(eventType, et => typeof(IBus).GetMethod(nameof(IBus.Publish), [et, typeof(string), typeof(byte?), typeof(CancellationToken)]));
            if (method is null)
            {
                return null;
            }
            var instanceParam = Expression.Parameter(typeof(IBus), "instance");
            var eventParam = Expression.Parameter(typeof(IEvent), "event");
            var routingKeyParam = Expression.Parameter(typeof(string), "routingKey");
            var priorityParam = Expression.Parameter(typeof(byte?), "priority");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
            var convertedEvent = Expression.Convert(eventParam, eventType);
            var methodCall = Expression.Call(instanceParam, method, convertedEvent, routingKeyParam, priorityParam, cancellationTokenParam);
            var lambda = Expression.Lambda<Func<IBus, IEvent, string?, byte?, CancellationToken, Task>>(methodCall, instanceParam, eventParam, routingKeyParam, priorityParam, cancellationTokenParam);
            return lambda.Compile();
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to create publish delegate for event type {EventType}", eventType.Name);
            }
            return null;
        }
    }
}
using EasilyNET.RabbitBus.AspNetCore.Abstractions;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.AspNetCore.Metrics;
using EasilyNET.RabbitBus.AspNetCore.Utilities;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasilyNET.RabbitBus.AspNetCore.Services;

/// <summary>
/// 消息确认管理器，负责消息确认和重试逻辑
/// </summary>
internal sealed class MessageConfirmService(
    EventPublisher eventPublisher,
    IBus iBus,
    ILogger<MessageConfirmService> logger,
    IOptionsMonitor<RabbitConfig> options,
    IDeadLetterStore deadLetterStore) : BackgroundService
{
    private const double DropTargetRatio = 0.5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int batchSize = 20;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 动态/可配置的重试队列上限
                var maxQueueSize = GetEffectiveMaxQueueSize();

                // 队列长度保护
                if (eventPublisher.NackedMessages.Count > maxQueueSize)
                {
                    var dropTarget = (int)(maxQueueSize * DropTargetRatio);
                    var dropped = 0;
                    while (eventPublisher.NackedMessages.Count > dropTarget && eventPublisher.NackedMessages.TryDequeue(out _))
                    {
                        dropped++;
                    }
                    if (dropped > 0)
                    {
                        RabbitBusMetrics.PublishDiscarded.Add(dropped);
                        if (logger.IsEnabled(LogLevel.Error))
                        {
                            logger.LogError("Dropped {Count} nacked messages due to queue overflow (max={Max})", dropped, maxQueueSize);
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
                        RabbitBusMetrics.PublishDiscarded.Add(1);
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
                        // 直接调用 IBus.Publish (非泛型重载)，避免反射和动态代码生成，支持 AOT
                        await iBus.Publish(item.Event, eventType, item.RoutingKey, item.Priority, stoppingToken);
                        RabbitBusMetrics.PublishRetried.Add(1);
                    }
                    catch (Exception ex)
                    {
                        if (logger.IsEnabled(LogLevel.Error))
                        {
                            logger.LogError(ex, "Retry failed for {EventType} ID {EventId} attempt {Attempt}", item.Event.GetType().Name, item.Event.EventId, item.RetryCount);
                        }
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

    private int GetEffectiveMaxQueueSize()
    {
        var cfg = options.Get(Constant.OptionName);
        // 显式配置优先
        if (cfg.RetryQueueMaxSize > 0)
        {
            return cfg.RetryQueueMaxSize;
        }
        // 动态根据内存估算
        var total = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        if (total <= 0)
        {
            total = 2L * 1024 * 1024 * 1024; // 兜底 2GB
        }
        var ratio = cfg.RetryQueueMaxMemoryRatio;
        if (ratio <= 0)
        {
            ratio = 0.02; // 默认 2%
        }
        if (ratio > 0.25)
        {
            ratio = 0.25; // 上限 25%
        }
        var budget = (long)(total * ratio);
        var est = cfg.RetryQueueAvgEntryBytes;
        if (est <= 0)
        {
            est = 2048; // 默认每条 ~2KB 估算
        }
        var calculated = (int)Math.Clamp(budget / est, 1_000, 500_000);
        return calculated;
    }
}
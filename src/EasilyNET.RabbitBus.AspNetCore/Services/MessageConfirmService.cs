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
internal sealed class MessageConfirmService(EventPublisher eventPublisher, IBus iBus, ILogger<MessageConfirmService> logger, IOptionsMonitor<RabbitConfig> options, IDeadLetterStore deadLetterStore) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retryReader = eventPublisher.RetryMessageReader;

        // 使用 Channel 的异步枚举模式
        await foreach (var msg in retryReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var now = DateTime.UtcNow;

                // 如果还没到重试时间，延迟后重新入队
                if (msg.NextRetryTime > now)
                {
                    var delay = msg.NextRetryTime - now;
                    if (delay.TotalMilliseconds > 0)
                    {
                        await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                    }
                    // 延迟完成后直接继续处理本条消息即可。
                    // Channel<T> 没有“放回队列头部”的语义，且重新入队会引入额外调度/循环。
                }

                // 处理重试消息
                var rabbitCfg = options.Get(Constant.OptionName);
                var maxRetries = rabbitCfg.RetryCount;
                if (msg.RetryCount > maxRetries)
                {
                    // 超过最大重试次数，写入死信
                    RabbitBusMetrics.PublishDiscarded.Add(1);
                    RabbitBusMetrics.DeadLettered.Add(1);
                    try
                    {
                        await deadLetterStore.StoreAsync(new DeadLetterMessage(msg.Event.GetType().Name,
                                msg.Event.EventId,
                                DateTime.UtcNow,
                                msg.RetryCount,
                                msg.Event),
                            stoppingToken).ConfigureAwait(false);
                    }
                    catch (Exception dlEx)
                    {
                        if (logger.IsEnabled(LogLevel.Error))
                        {
                            logger.LogError(dlEx, "Failed to store dead-letter message {EventType} ID {EventId}",
                                msg.Event.GetType().Name, msg.Event.EventId);
                        }
                    }
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Discarding message {EventType} ID {EventId} after {RetryCount} retries (limit {Limit})",
                            msg.Event.GetType().Name, msg.Event.EventId, msg.RetryCount, maxRetries);
                    }
                    continue;
                }

                // 重试发布消息
                try
                {
                    var eventType = msg.Event.GetType();
                    await iBus.Publish(msg.Event, eventType, msg.RoutingKey, msg.Priority, stoppingToken).ConfigureAwait(false);
                    RabbitBusMetrics.PublishRetried.Add(1);
                }
                catch (Exception ex)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError(ex, "Retry failed for {EventType} ID {EventId} attempt {Attempt}",
                            msg.Event.GetType().Name, msg.Event.EventId, msg.RetryCount);
                    }
                    // 重新调度重试
                    var next = DateTime.UtcNow + BackoffUtility.Exponential(msg.RetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
                    await eventPublisher.EnqueueRetryAsync(msg with
                    {
                        RetryCount = msg.RetryCount + 1,
                        NextRetryTime = next
                    }, stoppingToken).ConfigureAwait(false);
                    RabbitBusMetrics.RetryEnqueued.Add(1);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Unexpected error processing retry message {EventType} ID {EventId}",
                        msg.Event.GetType().Name, msg.Event.EventId);
                }
                await Task.Delay(2000, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
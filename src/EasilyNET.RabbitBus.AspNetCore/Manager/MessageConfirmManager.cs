using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 消息确认管理器，负责消息确认和重试逻辑
/// </summary>
internal sealed class MessageConfirmManager(EventPublisher eventPublisher, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pipelineProvider, IOptionsMonitor<RabbitConfig> options)
{
    private readonly ConcurrentDictionary<Type, Func<EventBus, IEvent, string?, byte?, CancellationToken, Task>?> _publishDelegateCache = [];
    private readonly ConcurrentDictionary<Type, MethodInfo?> _publishMethodCache = [];

    public async Task StartNackedMessageRetryTask(EventBus eventBus, CancellationTokenSource cancellationTokenSource)
    {
        var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
        const int maxQueueSize = 10000; // 限制重试队列最大大小
        const int batchSize = 10;       // 每次处理的消息数量
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                // 检查重试队列大小，避免内存溢出
                if (eventPublisher.NackedMessages.Count > maxQueueSize)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError("Nacked message queue size exceeded {MaxQueueSize}, clearing old messages", maxQueueSize);
                    }
                    // 清理最旧的消息
                    while (eventPublisher.NackedMessages.Count > maxQueueSize / 2 && eventPublisher.NackedMessages.TryDequeue(out _))
                    {
                        // 丢弃旧消息
                    }
                    await Task.Delay(5000, cancellationTokenSource.Token); // 等待5秒后再继续
                    continue;
                }

                // 批量处理重试消息，避免高频场景下的性能问题
                var messagesToRetry = new List<(IEvent Event, string? RoutingKey, byte? Priority, int RetryCount, DateTime NextRetryTime)>();
                while (messagesToRetry.Count < batchSize && eventPublisher.NackedMessages.TryPeek(out var nackedMessage))
                {
                    if (nackedMessage.NextRetryTime <= DateTime.UtcNow)
                    {
                        if (eventPublisher.NackedMessages.TryDequeue(out nackedMessage))
                        {
                            messagesToRetry.Add(nackedMessage);
                        }
                    }
                    else
                    {
                        break; // 消息还未到重试时间
                    }
                }

                // 处理这批消息
                foreach (var nackedMessage in messagesToRetry)
                {
                    var rabbitConfig = options.Get(Constant.OptionName);
                    var maxRetries = rabbitConfig.RetryCount;
                    if (nackedMessage.RetryCount > maxRetries)
                    {
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning("Message {EventType} with ID {EventId} exceeded maximum retry attempts ({MaxRetries}), giving up", nackedMessage.Event.GetType().Name, nackedMessage.Event.EventId, maxRetries);
                        }
                        continue;
                    }
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Retrying nacked message {EventType} with ID {EventId}, attempt {RetryCount}/{MaxRetries}", nackedMessage.Event.GetType().Name, nackedMessage.Event.EventId, nackedMessage.RetryCount, maxRetries);
                    }
                    try
                    {
                        var eventType = nackedMessage.Event.GetType();
                        var publishDelegate = _publishDelegateCache.GetOrAdd(eventType, CreatePublishDelegate);
                        if (publishDelegate is not null)
                        {
                            await pipeline.ExecuteAsync(async ct => await publishDelegate(eventBus, nackedMessage.Event, nackedMessage.RoutingKey, nackedMessage.Priority, ct), cancellationTokenSource.Token);
                        }
                        else if (logger.IsEnabled(LogLevel.Error))
                        {
                            logger.LogError("Unable to create publish delegate for event type {EventType}", eventType.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (logger.IsEnabled(LogLevel.Error))
                        {
                            logger.LogError(ex, "Failed to retry message {EventType} with ID {EventId} after retries", nackedMessage.Event.GetType().Name, nackedMessage.Event.EventId);
                        }
                        var backoff = Math.Pow(2, nackedMessage.RetryCount) * 1000;
                        var nextRetryTime = DateTime.UtcNow.AddMilliseconds(Math.Min(backoff, 30000));
                        eventPublisher.NackedMessages.Enqueue((nackedMessage.Event, nackedMessage.RoutingKey, nackedMessage.Priority, nackedMessage.RetryCount + 1, nextRetryTime));
                    }
                }

                // 如果没有消息需要处理，等待一段时间
                if (messagesToRetry.Count == 0)
                {
                    await Task.Delay(1000, cancellationTokenSource.Token);
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
                    logger.LogError(ex, "Error in nacked message retry task");
                }
            }
        }
    }

    private Func<EventBus, IEvent, string?, byte?, CancellationToken, Task>? CreatePublishDelegate(Type eventType)
    {
        try
        {
            var method = _publishMethodCache.GetOrAdd(eventType, et => typeof(EventBus).GetMethod(nameof(IBus.Publish), [et, typeof(string), typeof(byte?), typeof(CancellationToken)]));
            if (method is null)
            {
                return null;
            }
            var instanceParam = Expression.Parameter(typeof(EventBus), "instance");
            var eventParam = Expression.Parameter(typeof(IEvent), "event");
            var routingKeyParam = Expression.Parameter(typeof(string), "routingKey");
            var priorityParam = Expression.Parameter(typeof(byte?), "priority");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
            var convertedEvent = Expression.Convert(eventParam, eventType);
            var methodCall = Expression.Call(instanceParam, method, convertedEvent, routingKeyParam, priorityParam, cancellationTokenParam);
            var lambda = Expression.Lambda<Func<EventBus, IEvent, string?, byte?, CancellationToken, Task>>(methodCall, instanceParam, eventParam, routingKeyParam, priorityParam, cancellationTokenParam);
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
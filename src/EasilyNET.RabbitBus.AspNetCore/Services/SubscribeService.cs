using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.AspNetCore.Metrics;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasilyNET.RabbitBus.AspNetCore.Services;

/// <summary>
///     <para xml:lang="en">Background service for event subscription and message retry</para>
///     <para xml:lang="zh">用于事件订阅和消息重试的后台服务</para>
/// </summary>
internal sealed class SubscribeService(IServiceProvider sp, PersistentConnection connection, ILogger<SubscribeService> logger) : BackgroundService
{
    private readonly RabbitConfig _config = sp.GetRequiredService<IOptionsMonitor<RabbitConfig>>().Get(Constant.OptionName);

    /// <summary>
    ///     <para xml:lang="en">Executes the background service to subscribe to events and handle retries</para>
    ///     <para xml:lang="zh">执行后台服务以订阅事件并处理重试</para>
    /// </summary>
    /// <param name="cancelToken">
    ///     <para xml:lang="en">Token to monitor for cancellation requests</para>
    ///     <para xml:lang="zh">用于监控取消请求的令牌</para>
    /// </param>
    protected override async Task ExecuteAsync(CancellationToken cancelToken)
    {
        // 初始化连接并启动消费者
        await connection.InitializeAsync(cancelToken);
        var ibus = sp.GetRequiredService<IBus>() as EventBus ?? throw new InvalidOperationException("IBus service is not registered or is not of type EventBus.");
        var publisher = sp.GetRequiredService<EventPublisher>();
        var consumerTask = ibus.RunRabbit(cancelToken);

        // 启动后台重试任务
        var retryTask = ProcessNackedMessagesAsync(publisher, ibus, cancelToken);

        // 等待两个任务完成
        await Task.WhenAll(consumerTask, retryTask);
    }

    /// <summary>
    /// 持续处理进入NACK队列的消息
    /// </summary>
    private async Task ProcessNackedMessagesAsync(EventPublisher publisher, IBus bus, CancellationToken ct)
    {
        // 从配置中获取重试间隔，提供一个合理的默认值
        var retryInterval = TimeSpan.FromSeconds(_config.RetryIntervalSeconds > 0 ? _config.RetryIntervalSeconds : 1);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting NACKed message processor with check interval: {Interval}", retryInterval);
        }
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(retryInterval, ct);
                // 检查是否有待处理的消息
                if (publisher.NackedMessages.IsEmpty)
                {
                    continue;
                }
                // 尝试从队列中取出一条消息进行处理
                if (!publisher.NackedMessages.TryDequeue(out var messageToRetry))
                {
                    continue;
                }
                // 检查消息是否到达重试时间
                if (DateTime.UtcNow < messageToRetry.NextRetryTime)
                {
                    // 未到重试时间，将消息重新放回队列尾部
                    publisher.NackedMessages.Enqueue(messageToRetry);
                    continue;
                }
                // 检查是否超过最大重试次数
                if (messageToRetry.RetryCount > _config.RetryCount)
                {
                    logger.LogWarning("Event {EventId} has exceeded max retry count of {MaxRetries} and will be discarded.", messageToRetry.Event.EventId, _config.RetryCount);
                    RabbitBusMetrics.PublishDiscarded.Add(1);
                    continue;
                }
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Retrying event {EventId}, attempt {Attempt}/{MaxRetries}", messageToRetry.Event.EventId, messageToRetry.RetryCount, _config.RetryCount);
                }
                try
                {
                    // 重新发布消息。注意：这里我们不关心消息是普通还是延迟，因为重试逻辑是统一的。
                    // Publish方法内部会处理超时并再次将其放入NACK队列。
                    await bus.Publish(messageToRetry.Event, messageToRetry.RoutingKey, messageToRetry.Priority, ct);
                    RabbitBusMetrics.PublishRetried.Add(1);
                }
                catch (Exception ex)
                {
                    // Publish方法已经将失败的消息重新入队，这里只记录异常
                    logger.LogError(ex, "An exception occurred while retrying event {EventId}. It will be re-queued by the publisher.", messageToRetry.Event.EventId);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                logger.LogInformation("NACKed message processor is shutting down.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred in the NACKed message processor loop.");
                // 增加一个较长的延迟，避免在持续出错时消耗过多CPU
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }
}
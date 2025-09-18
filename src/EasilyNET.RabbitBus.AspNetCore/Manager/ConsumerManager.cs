using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 消费者管理器，负责消费者初始化和管理
/// </summary>
internal sealed class ConsumerManager(PersistentConnection conn, EventConfigurationRegistry eventRegistry, EventHandlerInvoker handlerInvoker, ILogger<EventBus> logger, IOptionsMonitor<RabbitConfig> options)
{
    /// <summary>
    /// 初始化所有启用的事件消费者。
    /// </summary>
    /// <param name="handleReceivedEvent">消息处理委托</param>
    /// <param name="cancellationToken">取消令牌源</param>
    public async Task InitializeConsumers(Func<Type, BasicDeliverEventArgs, IChannel, int, CancellationToken, Task> handleReceivedEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handleReceivedEvent);
        var configs = eventRegistry.GetAllConfigurations();
        var startupTasks = new List<Task>();
        foreach (var config in configs)
        {
            if (!config.Enabled)
            {
                continue; // 已禁用
            }
            var eventType = config.EventType;
            if (eventType == typeof(IEvent))
            {
                continue; // 跳过基接口
            }

            // 过滤处理器 (忽略被忽略的处理器) 并去重
            if (config.Handlers.Count == 0)
            {
                continue;
            }
            HashSet<Type>? ignored = config.IgnoredHandlers.Count > 0 ? new(config.IgnoredHandlers) : null;
            var handlerTypes = config.Handlers.Where(ht => ignored is null || !ignored.Contains(ht)).Distinct().ToList();
            if (handlerTypes.Count == 0)
            {
                continue;
            }

            // 缓存事件->处理器映射（线程安全字典，重复赋值覆盖即可）
            handlerInvoker.EventHandlerCache[eventType] = handlerTypes;
            var consumerCount = Math.Max(1, config.HandlerThreadCount);
            for (var i = 0; i < consumerCount; i++)
            {
                startupTasks.Add(StartConsumerAsync(config, eventType, i, handleReceivedEvent, cancellationToken));
            }
        }

        // 等待所有消费者启动完成（至少完成初始声明及 BasicConsume 注册）
        await Task.WhenAll(startupTasks);
    }

    /// <summary>
    /// 启动单个消费者通道及其 BasicConsume。
    /// </summary>
    private async Task StartConsumerAsync(EventConfiguration config, Type eventType, int consumerIndex, Func<Type, BasicDeliverEventArgs, IChannel, int, CancellationToken, Task> handleReceivedEvent, CancellationToken ct)
    {
        try
        {
            await using var channel = await conn.GetChannelAsync(ct).ConfigureAwait(false);
            await DeclareExchangeIfNeeded(config, channel, ct).ConfigureAwait(false);
            await channel.QueueDeclareAsync(config.Queue.Name, config.Queue.Durable, config.Queue.Exclusive, config.Queue.AutoDelete, config.Queue.Arguments, cancellationToken: ct).ConfigureAwait(false);

            // 绑定并开始消费
            if (config.Exchange.Type != EModel.None)
            {
                await channel.QueueBindAsync(config.Queue.Name, config.Exchange.Name, config.Exchange.RoutingKey, cancellationToken: ct).ConfigureAwait(false);
            }
            await StartBasicConsume(eventType, config, channel, consumerIndex, handleReceivedEvent, ct).ConfigureAwait(false);

            // 当通道关闭或异常时，自动重建消费者
            channel.CallbackExceptionAsync += async (_, ea) =>
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ea.Exception, "Consumer channel callback exception for event {EventName} (idx={Index}), restarting consumer", eventType.Name, consumerIndex);
                }
                try
                {
                    if (!ct.IsCancellationRequested)
                    {
                        await StartConsumerAsync(config, eventType, consumerIndex, handleReceivedEvent, ct).ConfigureAwait(false);
                    }
                }
                catch
                {
                    /* ignore */
                }
            };
            channel.ChannelShutdownAsync += async (_, ea) =>
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Consumer channel shutdown for event {EventName} (idx={Index}). ReplyCode={ReplyCode}, ReplyText={ReplyText}. Restarting...", eventType.Name, consumerIndex, ea.ReplyCode, ea.ReplyText);
                }
                try
                {
                    if (!ct.IsCancellationRequested)
                    {
                        await StartConsumerAsync(config, eventType, consumerIndex, handleReceivedEvent, ct).ConfigureAwait(false);
                    }
                }
                catch
                {
                    /* ignore */
                }
            };

            // 等待取消信号，而不是轮询
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 取消信号，正常退出
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，无需记录
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to start consumer {ConsumerIndex} for event {EventName}", consumerIndex, eventType.Name);
            }
            // 可选择性地延迟并尝试重启
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200), ct).ConfigureAwait(false);
                if (!ct.IsCancellationRequested)
                {
                    await StartConsumerAsync(config, eventType, consumerIndex, handleReceivedEvent, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception oex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(oex, "Failed to restart consumer {ConsumerIndex} for event {EventName}", consumerIndex, eventType.Name);
                }
            }
        }
    }

    private static async Task DeclareExchangeIfNeeded(EventConfiguration config, IChannel channel, CancellationToken ct)
    {
        if (config.Exchange.Type == EModel.None)
        {
            return;
        }
        var exchangeArgs = new Dictionary<string, object?>(config.Exchange.Arguments);
        if (config.Exchange.Type == EModel.Delayed)
        {
            exchangeArgs.TryAdd("x-delayed-type", "direct");
        }
        await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, exchangeArgs, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 启动 BasicConsume 并保持直到取消或通道关闭。
    /// </summary>
    private async Task StartBasicConsume(Type eventType, EventConfiguration config, IChannel channel, int consumerIndex, Func<Type, BasicDeliverEventArgs, IChannel, int, CancellationToken, Task> handleReceivedEvent, CancellationToken ct)
    {
        if (!handlerInvoker.EventHandlerCache.TryGetValue(eventType, out var handlerTypes) || handlerTypes.Count == 0)
        {
            return;
        }
        var rabbitConfig = options.Get(Constant.OptionName); // 单次读取
        await ConfigureQosIfNeeded(config, channel, rabbitConfig.Qos, ct).ConfigureAwait(false);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await handleReceivedEvent(eventType, ea, channel, consumerIndex, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error handling message for event {EventName} on consumer {ConsumerIndex}", eventType.Name, consumerIndex);
                }
            }
        };
        await channel.BasicConsumeAsync(config.Queue.Name, false, consumer, ct).ConfigureAwait(false);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Started consumer {ConsumerIndex} for event {EventName}", consumerIndex, eventType.Name);
        }
    }

    private static async Task ConfigureQosIfNeeded(EventConfiguration config, IChannel channel, QosConfig defaultQos, CancellationToken ct)
    {
        var qosToUse = config.Qos.PrefetchCount > 0 ? config.Qos : defaultQos;
        if (qosToUse.PrefetchCount > 0)
        {
            await channel.BasicQosAsync(qosToUse.PrefetchSize, qosToUse.PrefetchCount, qosToUse.Global, ct).ConfigureAwait(false);
        }
    }
}
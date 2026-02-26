using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Utilities;
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
        var retryCount = 0;
        while (!ct.IsCancellationRequested)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            PersistentConnection.ChannelLease? lease = null;
            try
            {
                lease = await conn.CreateDedicatedChannelAsync(linkedCts.Token).ConfigureAwait(false);
                var channel = lease.Channel;
                await DeclareExchangeIfNeeded(config, channel, linkedCts.Token).ConfigureAwait(false);
                await channel.QueueDeclareAsync(config.Queue.Name, config.Queue.Durable, config.Queue.Exclusive, config.Queue.AutoDelete, config.Queue.Arguments, cancellationToken: linkedCts.Token).ConfigureAwait(false);
                if (config.Exchange.Type != EModel.None)
                {
                    // Headers exchange uses binding arguments (x-match + matching key-value pairs) instead of routing key
                    var bindingArgs = config.Exchange is { Type: EModel.Headers, BindingArguments.Count: > 0 }
                                          ? new Dictionary<string, object?>(config.Exchange.BindingArguments)
                                          : null;
                    await channel.QueueBindAsync(config.Queue.Name, config.Exchange.Name, config.Exchange.RoutingKey, bindingArgs, cancellationToken: linkedCts.Token).ConfigureAwait(false);
                }
                await StartBasicConsume(eventType, config, channel, consumerIndex, handleReceivedEvent, linkedCts.Token).ConfigureAwait(false);

                // 成功启动后重置重试计数
                retryCount = 0;

                Task CallbackExceptionAsync(object? _, CallbackExceptionEventArgs ea)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning(ea.Exception, "Consumer channel callback exception for event {EventName} (idx={Index}), restarting consumer", eventType.Name, consumerIndex);
                    }
                    // ReSharper disable once AccessToDisposedClosure
                    linkedCts.Cancel();
                    return Task.CompletedTask;
                }

                Task ChannelShutdownAsync(object? _, ShutdownEventArgs ea)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Consumer channel shutdown for event {EventName} (idx={Index}). ReplyCode={ReplyCode}, ReplyText={ReplyText}. Restarting...", eventType.Name, consumerIndex, ea.ReplyCode, ea.ReplyText);
                    }
                    // ReSharper disable once AccessToDisposedClosure
                    linkedCts.Cancel();
                    return Task.CompletedTask;
                }

                channel.CallbackExceptionAsync += CallbackExceptionAsync;
                channel.ChannelShutdownAsync += ChannelShutdownAsync;
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // 取消或通道事件触发，跳出循环以重建
                }
                finally
                {
                    channel.CallbackExceptionAsync -= CallbackExceptionAsync;
                    channel.ChannelShutdownAsync -= ChannelShutdownAsync;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break; // 全局取消
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Failed to start consumer {ConsumerIndex} for event {EventName}", consumerIndex, eventType.Name);
                }

                // 指数退避
                var delay = BackoffUtility.Exponential(retryCount, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
                retryCount++;
                try
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            finally
            {
                if (lease is not null)
                {
                    try
                    {
                        await lease.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning(ex, "Exception occurred while disposing channel lease for consumer {ConsumerIndex} for event {EventName}", consumerIndex, eventType.Name);
                        }
                    }
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
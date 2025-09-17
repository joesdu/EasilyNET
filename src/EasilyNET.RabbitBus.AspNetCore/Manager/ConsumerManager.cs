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
internal sealed class ConsumerManager(PersistentConnection conn, EventConfigurationRegistry eventRegistry, ILogger<EventBus> logger, IOptionsMonitor<RabbitConfig> options, CacheManager cacheManager)
{
    public async Task InitializeConsumers(Func<Type, BasicDeliverEventArgs, IChannel, int, CancellationToken, Task> handleReceivedEvent, CancellationTokenSource cancellationTokenSource)
    {
        var configs = eventRegistry.GetAllConfigurations();
        foreach (var config in configs)
        {
            if (!config.Enabled)
            {
                continue;
            }
            var eventType = config.EventType;
            if (eventType == typeof(IEvent))
            {
                continue;
            }
            var handlerTypes = config.Handlers.Where(ht => !config.IgnoredHandlers.Contains(ht)).Distinct().ToList();
            if (handlerTypes.Count == 0)
            {
                continue;
            }

            // 根据 HandlerThreadCount 创建多个消费者
            var consumerCount = Math.Max(1, config.HandlerThreadCount);
            for (var i = 0; i < consumerCount; i++)
            {
                var consumerIndex = i; // 捕获循环变量
                await Task.Factory.StartNew(async () =>
                {
                    var ct = cancellationTokenSource.Token;
                    await using var channel = await CreateConsumerChannel(config, ct);
                    cacheManager.EventHandlerCache[eventType] = handlerTypes;
                    if (config.Exchange.Type != EModel.None)
                    {
                        await channel.QueueBindAsync(config.Queue.Name, config.Exchange.Name, config.Exchange.RoutingKey, cancellationToken: ct);
                    }
                    await StartBasicConsume(eventType, config, channel, consumerIndex, handleReceivedEvent, ct);
                }, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
    }

    private async Task<IChannel> CreateConsumerChannel(EventConfiguration config, CancellationToken ct)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Creating consumer channel");
        }
        var channel = await conn.GetChannelAsync();
        await DeclareExchangeIfNeeded(config, channel, ct);
        await channel.QueueDeclareAsync(config.Queue.Name, config.Queue.Durable, config.Queue.Exclusive, config.Queue.AutoDelete, config.Queue.Arguments, cancellationToken: ct);
        channel.CallbackExceptionAsync += async (_, ea) =>
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ea.Exception, "Recreating consumer channel");
            }
            cacheManager.EventHandlerCache.Clear();
            await InitializeConsumers(null!, new()); // 需要重新初始化
        };
        return channel;
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
        await channel.ExchangeDeclareAsync(config.Exchange.Name, config.Exchange.Type.Description, config.Exchange.Durable, config.Exchange.AutoDelete, exchangeArgs, cancellationToken: ct);
    }

    private async Task StartBasicConsume(Type eventType, EventConfiguration config, IChannel channel, int consumerIndex, Func<Type, BasicDeliverEventArgs, IChannel, int, CancellationToken, Task> handleReceivedEvent, CancellationToken ct)
    {
        if (!cacheManager.EventHandlerCache.TryGetValue(eventType, out var handlerTypes) || handlerTypes.Count == 0)
        {
            return;
        }
        var rabbitConfig = options.Get(Constant.OptionName);
        await ConfigureQosIfNeeded(config, channel, rabbitConfig.Qos, ct);
        var consumer = new AsyncEventingBasicConsumer(channel);
        await channel.BasicConsumeAsync(config.Queue.Name, false, consumer, ct);
        consumer.ReceivedAsync += async (_, ea) => await handleReceivedEvent(eventType, ea, channel, consumerIndex, ct);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Started consumer {ConsumerIndex} for event {EventName}", consumerIndex, eventType.Name);
        }
        while (!channel.IsClosed)
        {
            await Task.Delay(100000, ct);
        }
    }

    private static async Task ConfigureQosIfNeeded(EventConfiguration config, IChannel channel, QosConfig defaultQos, CancellationToken ct)
    {
        var qosToUse = config.Qos.PrefetchCount > 0 ? config.Qos : defaultQos;
        if (qosToUse.PrefetchCount > 0)
        {
            await channel.BasicQosAsync(qosToUse.PrefetchSize, qosToUse.PrefetchCount, qosToUse.Global, ct);
        }
    }
}
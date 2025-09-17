using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 事件处理器调用器，负责事件处理逻辑
/// </summary>
internal sealed class EventHandlerInvoker(IServiceProvider sp, IBusSerializer serializer, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pipelineProvider)
{
    private const string HandleName = nameof(IEventHandler<>.HandleAsync);
    private readonly ConcurrentDictionary<(Type HandlerType, Type EventType), Func<object, Task>?> _handleAsyncDelegateCache = [];

    public async Task HandleReceivedEvent(Type eventType, BasicDeliverEventArgs ea, IChannel channel, int consumerIndex, ConcurrentDictionary<Type, List<Type>> eventHandlerCache, CancellationToken ct)
    {
        try
        {
            await ProcessEvent(eventType, ea.Body.Span.ToArray(), async () =>
            {
                try
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct).ConfigureAwait(false);
                }
                catch (ObjectDisposedException ex)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError(ex, "Channel disposed before ACK, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                    }
                }
            }, consumerIndex, eventHandlerCache, ct);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error processing message, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
            }
        }
    }

    private async Task ProcessEvent(Type eventType, byte[] message, Func<ValueTask> ack, int consumerIndex, ConcurrentDictionary<Type, List<Type>> eventHandlerCache, CancellationToken ct)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Processing event: {EventName} on consumer {ConsumerIndex}", eventType.Name, consumerIndex);
        }
        if (!eventHandlerCache.TryGetValue(eventType, out var handlerTypes) || handlerTypes.Count == 0)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("No subscriptions for event: {EventName}", eventType.Name);
            }
            return;
        }
        var @event = serializer.Deserialize(message, eventType);
        if (@event is null)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("Failed to deserialize event: {EventName}", eventType.Name);
            }
            return;
        }
        using var scope = sp.GetService<IServiceScopeFactory>()?.CreateScope();

        // 每个消息只被一个消费者处理，每个handler执行一次
        foreach (var handlerType in handlerTypes)
        {
            var cachedDelegate = GetOrCreateHandlerDelegate(handlerType, eventType, scope);
            if (cachedDelegate is null)
            {
                continue;
            }
            var pipeline = pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
            try
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Consumer {ConsumerIndex} executing handler {HandlerType} for event {EventName}", consumerIndex, handlerType.Name, eventType.Name);
                }
                await pipeline.ExecuteAsync(async _ => await cachedDelegate(@event), ct);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error executing handler {HandlerType} for event: {EventName}", handlerType.Name, eventType.Name);
                }
                throw;
            }
        }
        await ack.Invoke();
    }

    private Func<object, Task>? GetOrCreateHandlerDelegate(Type handlerType, Type eventType, IServiceScope? scope)
    {
        var key = (handlerType, eventType);
        // ReSharper disable once InvertIf
        if (!_handleAsyncDelegateCache.TryGetValue(key, out var cachedDelegate))
        {
            var method = handlerType.GetMethod(HandleName, [eventType]);
            if (method is null)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError("Handler method not found for event: {EventName}", eventType.Name);
                }
                return null;
            }
            var handler = scope?.ServiceProvider.GetService(handlerType);
            if (handler is null)
            {
                return null;
            }
            cachedDelegate = CreateHandleAsyncDelegate(handler, method, eventType);
            _handleAsyncDelegateCache[key] = cachedDelegate;
        }
        return cachedDelegate;
    }

    private static Func<object, Task> CreateHandleAsyncDelegate(object handler, MethodInfo method, Type eventType)
    {
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var eventParam = Expression.Parameter(typeof(object), "event");
        var convertedHandler = Expression.Convert(handlerParam, handler.GetType());
        var convertedEvent = Expression.Convert(eventParam, eventType);
        var methodCall = Expression.Call(convertedHandler, method, convertedEvent);
        var lambda = Expression.Lambda<Func<object, object, Task>>(methodCall, handlerParam, eventParam);
        var compiledDelegate = lambda.Compile();
        return async @event =>
        {
            var task = compiledDelegate(handler, @event) ?? throw new InvalidOperationException($"Handler method '{method.Name}' for event type '{eventType.Name}' returned null Task.");
            await task.ConfigureAwait(false);
        };
    }
}
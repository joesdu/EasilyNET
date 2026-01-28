using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 事件处理器调用器，负责事件处理逻辑
/// </summary>
internal sealed class EventHandlerInvoker(IServiceProvider sp, IBusSerializer serializer, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pipelineProvider, EventConfigurationRegistry eventRegistry)
{
    private const string HandleName = nameof(IEventHandler<>.HandleAsync); // 名称解析一次

    // 缓存 (HandlerType, EventType) -> 开放委托: (object handler, object evt) => Task
    private static readonly ConcurrentDictionary<(Type HandlerType, Type EventType), Func<object, object, Task>> _openDelegateCache = new();

    // 事件配置注册器
    private readonly ResiliencePipeline _pipeline = pipelineProvider.GetPipeline(Constant.HandlerPipelineName);

    // 作用域工厂与管道缓存，减少每次消息的服务解析开销
    private readonly IServiceScopeFactory? _scopeFactory = sp.GetService<IServiceScopeFactory>();

    public ConcurrentDictionary<Type, List<Type>> EventHandlerCache { get; } = [];

    /// <summary>
    /// 清除事件处理器相关缓存
    /// </summary>
    public void ClearEventHandlerCaches()
    {
        EventHandlerCache.Clear();
    }

    /// <summary>
    /// 顶层入口: 收到消息 -> 反序列化 -> 逐个 Handler 处理 -> Ack
    /// </summary>
    public async Task HandleReceivedEvent(Type eventType, BasicDeliverEventArgs ea, IChannel channel, int consumerIndex, ConcurrentDictionary<Type, List<Type>> eventHandlerCache, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }
        try
        {
            // 尽量避免重复拷贝 Body
            var bodyBytes = GetBodyBytes(ea.Body);
            var processed = false;
            try
            {
                processed = await ProcessEventAsync(eventType, bodyBytes, consumerIndex, eventHandlerCache, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw; // 重新抛出以便外层处理
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error processing message, DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                }
            }
            if (processed)
            {
                await AckAsync(channel, ea.DeliveryTag, ct).ConfigureAwait(false);
            }
            else
            {
                await NackAsync(channel, ea.DeliveryTag, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(ex, "Event handling was canceled.");
            }
        }
    }

    private async Task AckAsync(IChannel channel, ulong deliveryTag, CancellationToken ct)
    {
        try
        {
            await channel.BasicAckAsync(deliveryTag, false, ct).ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Channel disposed before ACK, DeliveryTag: {DeliveryTag}", deliveryTag);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "ACK failed for DeliveryTag: {DeliveryTag}", deliveryTag);
            }
        }
    }

    private async Task NackAsync(IChannel channel, ulong deliveryTag, CancellationToken ct)
    {
        try
        {
            await channel.BasicNackAsync(deliveryTag, false, false, ct).ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Channel disposed before NACK, DeliveryTag: {DeliveryTag}", deliveryTag);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "NACK failed for DeliveryTag: {DeliveryTag}", deliveryTag);
            }
        }
    }

    private async Task<bool> ProcessEventAsync(Type eventType, byte[] message, int consumerIndex, ConcurrentDictionary<Type, List<Type>> eventHandlerCache, CancellationToken ct)
    {
        if (!eventHandlerCache.TryGetValue(eventType, out var handlerTypes) || handlerTypes.Count == 0)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("No subscriptions for event: {EventName}", eventType.Name);
            }
            return false;
        }
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Processing event: {EventName} on consumer {ConsumerIndex}", eventType.Name, consumerIndex);
        }
        object? @event;
        try
        {
            @event = serializer.Deserialize(message, eventType);
            if (@event is null)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError("Failed to deserialize event: {EventName}", eventType.Name);
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Deserialization failure for event: {EventName}", eventType.Name);
            }
            return false;
        }
        using var scope = _scopeFactory?.CreateScope();
        var provider = scope?.ServiceProvider ?? sp; // 无作用域时回退到根容器

        // 检查是否要求顺序执行
        var config = eventRegistry.GetConfiguration(eventType);
        var shouldExecuteSequentially = config?.SequentialHandlerExecution == true;

        // 快速路径: 单个处理器
        if (handlerTypes.Count == 1)
        {
            await InvokeHandlerAsync(handlerTypes[0], eventType, provider, @event, consumerIndex, ct).ConfigureAwait(false);
        }
        else if (shouldExecuteSequentially)
        {
            // 顺序执行：确保处理器按注册顺序执行
            await ProcessHandlersSequentially(handlerTypes, eventType, provider, @event, consumerIndex, ct).ConfigureAwait(false);
        }
        else
        {
            // 并发执行：处理器并行执行（默认行为）
            await ProcessHandlersConcurrently(handlerTypes, eventType, provider, @event, consumerIndex, ct).ConfigureAwait(false);
        }
        return true;
    }

    /// <summary>
    /// 顺序执行所有处理器（保证执行顺序）
    /// </summary>
    private async Task ProcessHandlersSequentially(List<Type> handlerTypes, Type eventType, IServiceProvider provider, object @event, int consumerIndex, CancellationToken ct)
    {
        foreach (var handlerType in handlerTypes)
        {
            await InvokeHandlerAsync(handlerType, eventType, provider, @event, consumerIndex, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 并发执行所有处理器（提高吞吐量）
    /// </summary>
    private async Task ProcessHandlersConcurrently(List<Type> handlerTypes, Type eventType, IServiceProvider provider, object @event, int consumerIndex, CancellationToken ct)
    {
        var tasks = new List<Task>(handlerTypes.Count);
        tasks.AddRange(handlerTypes.Select(handlerType => InvokeHandlerAsync(handlerType, eventType, provider, @event, consumerIndex, ct)));
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {
            // 详细异常信息会在 InvokeHandlerAsync 中逐个记录。
            // 这里仅做汇总，避免重复打印异常栈，但让并发失败更容易定位。
            if (logger.IsEnabled(LogLevel.Error))
            {
                var failedHandlers = tasks.Count(static t => t.IsFaulted);
                logger.LogError("Concurrent handlers failed for event {EventName} on consumer {ConsumerIndex}. FailedHandlers={FailedHandlers}/{TotalHandlers}",
                    eventType.Name, consumerIndex, failedHandlers, handlerTypes.Count);
            }
            throw;
        }
    }

    private async Task InvokeHandlerAsync(Type handlerType, Type eventType, IServiceProvider provider, object @event, int consumerIndex, CancellationToken ct)
    {
        var handler = provider.GetService(handlerType);
        if (handler is null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Handler {HandlerType} not found in DI container for event {EventName}", handlerType.Name, eventType.Name);
            }
            return; // 可能未注册
        }
        var openDelegate = GetOrAddOpenDelegate(handlerType, eventType);
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Consumer {ConsumerIndex} executing handler {HandlerType} for event {EventName}", consumerIndex, handlerType.Name, eventType.Name);
            }

            // 使用 resilience pipeline 包裹执行
            await _pipeline.ExecuteAsync(async _ => await openDelegate(handler, @event).ConfigureAwait(false), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error executing handler {HandlerType} for event: {EventName}", handlerType.Name, eventType.Name);
            }
            throw; // 传播以便上层决定是否 Nack/重试 (当前逻辑: 传播会导致整体 catch, 不执行 ACK)
        }
    }

    private static Func<object, object, Task> GetOrAddOpenDelegate(Type handlerType, Type eventType)
    {
        return _openDelegateCache.GetOrAdd((handlerType, eventType), static key =>
        {
            var (hType, eType) = key;
            var method = hType.GetMethod(HandleName, [eType]) ?? throw new MissingMethodException(hType.FullName, HandleName);

            // (object handler, object evt) => ((TH)handler).HandleAsync((TE)evt)
            var handlerParam = Expression.Parameter(typeof(object), "handler");
            var evtParam = Expression.Parameter(typeof(object), "evt");
            var call = Expression.Call(Expression.Convert(handlerParam, hType), method, Expression.Convert(evtParam, eType));
            var lambda = Expression.Lambda<Func<object, object, Task>>(call, handlerParam, evtParam);
            return lambda.Compile();
        });
    }

    // 尽可能复用底层数组，避免不必要的拷贝
    private static byte[] GetBodyBytes(ReadOnlyMemory<byte> body)
    {
        // ReSharper disable once InvertIf
        if (MemoryMarshal.TryGetArray(body, out var segment) && segment.Array is not null)
        {
            // 仅当覆盖整个数组时直接返回 (避免暴露大数组的子段对 GC 产生延迟保留)
            if (segment.Offset == 0 && segment.Count == segment.Array.Length)
            {
                return segment.Array;
            }
        }
        return body.ToArray();
    }
}
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using EasilyNET.RabbitBus.AspNetCore.Abstractions;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 事件处理器调用器，负责事件处理逻辑
/// </summary>
internal sealed class EventHandlerInvoker(IServiceProvider sp, IBusSerializer serializer, ILogger<EventBus> logger, ResiliencePipelineProvider<string> pipelineProvider, EventConfigurationRegistry eventRegistry, IDeadLetterStore deadLetterStore, IOptionsMonitor<RabbitConfig> rabbitOptions)
{
    private const string HandleName = nameof(IEventHandler<>.HandleAsync); // 名称解析一次

    // 缓存 (HandlerType, EventType) -> 开放委托: (object handler, object evt) => Task
    private static readonly ConcurrentDictionary<(Type HandlerType, Type EventType), Func<object, object, Task>> _openDelegateCache = new();

    // 缓存中间件调用器（按事件类型），避免每条消息反射
    private static readonly ConcurrentDictionary<Type, MiddlewareInvoker> _middlewareInvokerCache = new();

    private static readonly ActivitySource s_activitySource = new(Constant.ActivitySourceName);

    // 缓存自定义弹性管道（按事件类型）
    private readonly ConcurrentDictionary<Type, ResiliencePipeline> _customPipelineCache = new();

    // 事件配置注册器
    private readonly ResiliencePipeline _defaultPipeline = pipelineProvider.GetPipeline(Constant.HandlerPipelineName);

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
    /// 顶层入口: 收到消息 -> 反序列化 -> 中间件 -> 逐个 Handler 处理 -> 回退 -> Ack/Nack
    /// </summary>
    public async Task HandleReceivedEvent(Type eventType, BasicDeliverEventArgs ea, IChannel channel, int consumerIndex, ConcurrentDictionary<Type, List<Type>> eventHandlerCache, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }
        try
        {
            // 提取父级追踪上下文并创建消费者 Activity
            var parentContext = ExtractParentTraceContext(ea.BasicProperties.Headers);
            using var activity = s_activitySource.StartActivity("rabbitmq.consume", ActivityKind.Consumer, parentContext);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.source", ea.Exchange);
            activity?.SetTag("messaging.destination", ea.RoutingKey);
            activity?.SetTag("messaging.consumer_id", consumerIndex.ToString());

            // 尽量避免重复拷贝 Body
            var bodyBytes = GetBodyBytes(ea.Body);
            var action = ConsumerAction.Nack;
            try
            {
                action = await ProcessEventAsync(eventType, bodyBytes, ea, consumerIndex, eventHandlerCache, ct).ConfigureAwait(false);
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
            switch (action)
            {
                case ConsumerAction.Ack:
                    await AckAsync(channel, ea.DeliveryTag, ct).ConfigureAwait(false);
                    break;
                case ConsumerAction.Requeue:
                    await NackAsync(channel, ea.DeliveryTag, true, ct).ConfigureAwait(false);
                    break;
                default: // Nack, DeadLetter (already handled)
                    await NackAsync(channel, ea.DeliveryTag, false, ct).ConfigureAwait(false);
                    break;
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

    private async Task NackAsync(IChannel channel, ulong deliveryTag, bool requeue, CancellationToken ct)
    {
        try
        {
            await channel.BasicNackAsync(deliveryTag, false, requeue, ct).ConfigureAwait(false);
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

    private async Task<ConsumerAction> ProcessEventAsync(Type eventType, byte[] message, BasicDeliverEventArgs ea, int consumerIndex, ConcurrentDictionary<Type, List<Type>> eventHandlerCache, CancellationToken ct)
    {
        if (!eventHandlerCache.TryGetValue(eventType, out var handlerTypes) || handlerTypes.Count == 0)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("No subscriptions for event: {EventName}", eventType.Name);
            }
            return ConsumerAction.Nack;
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
                return ConsumerAction.Nack;
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Deserialization failure for event: {EventName}", eventType.Name);
            }
            return ConsumerAction.Nack;
        }
        using var scope = _scopeFactory?.CreateScope();
        var provider = scope?.ServiceProvider ?? sp; // 无作用域时回退到根容器
        var config = eventRegistry.GetConfiguration(eventType);

        // 提取消息头
        var headers = ExtractHeaders(ea.BasicProperties.Headers);

        // 获取排序后的处理器列表
        var sortedHandlerTypes = GetSortedHandlerTypes(config, handlerTypes);

        // 获取弹性管道（自定义或默认）
        var pipeline = GetHandlerPipeline(config, eventType);
        try
        {
            // 中间件管道：如果配置了中间件，则包裹整个处理器链路
            if (config?.MiddlewareType is not null)
            {
                await ExecuteWithMiddlewareAsync(config, eventType, provider, @event, headers, sortedHandlerTypes, consumerIndex, pipeline, ct).ConfigureAwait(false);
            }
            else
            {
                await ExecuteHandlerChainAsync(sortedHandlerTypes, eventType, provider, @event, consumerIndex, config, pipeline, ct).ConfigureAwait(false);
            }
            return ConsumerAction.Ack;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 尝试回退处理器（此处为配置的最大重试次数，而非本次消息的实际重试次数）
            var maxRetryCount = rabbitOptions.Get(Constant.OptionName).RetryCount;
            return await TryFallbackAsync(config, eventType, provider, @event, ex, maxRetryCount, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 通过中间件执行处理器链路（使用缓存的委托避免每条消息反射）
    /// </summary>
    private async Task ExecuteWithMiddlewareAsync(EventConfiguration config, Type eventType, IServiceProvider provider, object @event, IReadOnlyDictionary<string, object?> headers, List<Type> sortedHandlerTypes, int consumerIndex, ResiliencePipeline pipeline, CancellationToken ct)
    {
        var middleware = provider.GetService(config.MiddlewareType!);
        if (middleware is null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Middleware {MiddlewareType} not found in DI container for event {EventName}, falling back to direct execution", config.MiddlewareType!.Name, eventType.Name);
            }
            await ExecuteHandlerChainAsync(sortedHandlerTypes, eventType, provider, @event, consumerIndex, config, pipeline, ct).ConfigureAwait(false);
            return;
        }
        var invoker = _middlewareInvokerCache.GetOrAdd(eventType, static eType => MiddlewareInvoker.Create(eType));
        var context = invoker.CreateContext(@event, headers, ct);
        await invoker.InvokeAsync(middleware, context, next).ConfigureAwait(false);
        return;
        Task next() => ExecuteHandlerChainAsync(sortedHandlerTypes, eventType, provider, @event, consumerIndex, config, pipeline, ct);
    }

    /// <summary>
    /// 执行处理器链路（支持顺序/并发执行和排序）
    /// </summary>
    private async Task ExecuteHandlerChainAsync(List<Type> handlerTypes, Type eventType, IServiceProvider provider, object @event, int consumerIndex, EventConfiguration? config, ResiliencePipeline pipeline, CancellationToken ct)
    {
        var shouldExecuteSequentially = config?.SequentialHandlerExecution == true;

        // 快速路径: 单个处理器
        if (handlerTypes.Count == 1)
        {
            await InvokeHandlerAsync(handlerTypes[0], eventType, provider, @event, consumerIndex, pipeline, ct).ConfigureAwait(false);
        }
        else if (shouldExecuteSequentially)
        {
            await ProcessHandlersSequentially(handlerTypes, eventType, provider, @event, consumerIndex, pipeline, ct).ConfigureAwait(false);
        }
        else
        {
            await ProcessHandlersConcurrently(handlerTypes, eventType, provider, @event, consumerIndex, pipeline, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 尝试调用回退处理器，返回消息处置动作
    /// </summary>
    private async Task<ConsumerAction> TryFallbackAsync(EventConfiguration? config, Type eventType, IServiceProvider provider, object @event, Exception exception, int retryCount, CancellationToken ct)
    {
        if (config?.FallbackHandlerType is null)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(exception, "Event {EventName} processing failed with no fallback handler configured", eventType.Name);
            }
            return ConsumerAction.Nack;
        }
        try
        {
            var fallback = provider.GetService(config.FallbackHandlerType);
            if (fallback is null)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Fallback handler {FallbackType} not found in DI for event {EventName}", config.FallbackHandlerType.Name, eventType.Name);
                }
                return ConsumerAction.Nack;
            }

            // 调用 IEventFallbackHandler<TEvent>.OnFallbackAsync
            var fallbackInterfaceType = typeof(IEventFallbackHandler<>).MakeGenericType(eventType);
            var fallbackMethod = fallbackInterfaceType.GetMethod(nameof(IEventFallbackHandler<>.OnFallbackAsync));
            if (fallbackMethod is null)
            {
                return ConsumerAction.Nack;
            }
            var task = (Task<ConsumerAction>)fallbackMethod.Invoke(fallback, [@event, exception, retryCount])!;
            var action = await task.ConfigureAwait(false);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Fallback handler returned {Action} for event {EventName}", action, eventType.Name);
            }

            // DeadLetter 需要先存储再 Ack
            if (action == ConsumerAction.DeadLetter)
            {
                var stored = await StoreDeadLetterAsync(@event, eventType, retryCount, ct).ConfigureAwait(false);
                return stored ? ConsumerAction.Ack : ConsumerAction.Nack;
            }
            return action;
        }
        catch (Exception fallbackEx)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(fallbackEx, "Fallback handler failed for event {EventName}", eventType.Name);
            }
            return ConsumerAction.Nack;
        }
    }

    /// <summary>
    /// 将消息存入死信存储并返回 true（Ack，因为已经存入死信）
    /// </summary>
    private async Task<bool> StoreDeadLetterAsync(object @event, Type eventType, int retryCount, CancellationToken ct)
    {
        try
        {
            if (@event is IEvent evt)
            {
                await deadLetterStore.StoreAsync(new DeadLetterMessage(eventType.Name, evt.EventId, DateTime.UtcNow, retryCount, evt), ct).ConfigureAwait(false);
            }
            return true; // Ack after dead-lettering
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to store dead-letter for event {EventName}", eventType.Name);
            }
            return false;
        }
    }

    /// <summary>
    /// 获取排序后的处理器类型列表
    /// </summary>
    private static List<Type> GetSortedHandlerTypes(EventConfiguration? config, List<Type> handlerTypes)
    {
        if (config?.OrderedHandlers.Count > 0)
        {
            return config.OrderedHandlers
                         .Where(h => config.IgnoredHandlers.Count == 0 || !config.IgnoredHandlers.Contains(h.HandlerType))
                         .OrderBy(h => h.Order)
                         .Select(h => h.HandlerType)
                         .ToList();
        }
        return handlerTypes;
    }

    /// <summary>
    /// 获取处理器弹性管道（自定义或默认）
    /// </summary>
    private ResiliencePipeline GetHandlerPipeline(EventConfiguration? config, Type eventType)
    {
        if (config?.CustomHandlerResilience is null)
        {
            return _defaultPipeline;
        }
        return _customPipelineCache.GetOrAdd(eventType, _ =>
        {
            var builder = new ResiliencePipelineBuilder();
            config.CustomHandlerResilience(builder);
            return builder.Build();
        });
    }

    /// <summary>
    /// 从 RabbitMQ BasicProperties 提取消息头
    /// </summary>
    private static IReadOnlyDictionary<string, object?> ExtractHeaders(IDictionary<string, object?>? headers) => headers is null or { Count: 0 } ? new Dictionary<string, object?>() : new Dictionary<string, object?>(headers);

    /// <summary>
    /// 从 RabbitMQ 消息头中提取父级追踪上下文（traceparent/tracestate）
    /// </summary>
    private static ActivityContext ExtractParentTraceContext(IDictionary<string, object?>? headers)
    {
        if (headers is null)
        {
            return default;
        }
        if (!headers.TryGetValue("traceparent", out var traceParent))
        {
            return default;
        }
        var traceParentStr = traceParent switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string str   => str,
            _            => null
        };
        if (traceParentStr is null)
        {
            return default;
        }
        {
            string? traceStateStr = null;
            if (headers.TryGetValue("tracestate", out var traceState))
            {
                traceStateStr = traceState switch
                {
                    byte[] bytes => Encoding.UTF8.GetString(bytes),
                    string str   => str,
                    _            => null
                };
            }
            if (ActivityContext.TryParse(traceParentStr, traceStateStr, out var context))
            {
                return context;
            }
        }
        return default;
    }

    /// <summary>
    /// 顺序执行所有处理器（保证执行顺序）
    /// </summary>
    private async Task ProcessHandlersSequentially(List<Type> handlerTypes, Type eventType, IServiceProvider provider, object @event, int consumerIndex, ResiliencePipeline pipeline, CancellationToken ct)
    {
        foreach (var handlerType in handlerTypes)
        {
            await InvokeHandlerAsync(handlerType, eventType, provider, @event, consumerIndex, pipeline, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 并发执行所有处理器（提高吞吐量）
    /// </summary>
    private async Task ProcessHandlersConcurrently(List<Type> handlerTypes, Type eventType, IServiceProvider provider, object @event, int consumerIndex, ResiliencePipeline pipeline, CancellationToken ct)
    {
        var tasks = new List<Task>(handlerTypes.Count);
        tasks.AddRange(handlerTypes.Select(handlerType => InvokeHandlerAsync(handlerType, eventType, provider, @event, consumerIndex, pipeline, ct)));
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

    private async Task InvokeHandlerAsync(Type handlerType, Type eventType, IServiceProvider provider, object @event, int consumerIndex, ResiliencePipeline pipeline, CancellationToken ct)
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
            await pipeline.ExecuteAsync(async _ => await openDelegate(handler, @event).ConfigureAwait(false), ct).ConfigureAwait(false);
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

    /// <summary>
    /// 缓存的中间件调用器，避免每条消息反射。按 eventType 缓存一次，后续调用零反射。
    /// </summary>
    private sealed class MiddlewareInvoker
    {
        private readonly Func<object, IReadOnlyDictionary<string, object?>, CancellationToken, object> _contextFactory;
        private readonly Func<object, object, Func<Task>, Task> _invokeDelegate;

        private MiddlewareInvoker(
            Func<object, IReadOnlyDictionary<string, object?>, CancellationToken, object> contextFactory,
            Func<object, object, Func<Task>, Task> invokeDelegate)
        {
            _contextFactory = contextFactory;
            _invokeDelegate = invokeDelegate;
        }

        /// <summary>
        /// 创建 EventContext 实例（已编译委托，无反射）
        /// </summary>
        public object CreateContext(object @event, IReadOnlyDictionary<string, object?> headers, CancellationToken ct) => _contextFactory(@event, headers, ct);

        /// <summary>
        /// 调用中间件的 HandleAsync（已编译委托，无反射）
        /// </summary>
        public Task InvokeAsync(object middleware, object context, Func<Task> next) => _invokeDelegate(middleware, context, next);

        /// <summary>
        /// 为指定事件类型编译中间件调用器（仅执行一次）
        /// </summary>
        public static MiddlewareInvoker Create(Type eventType)
        {
            // 编译 context 工厂: (object evt, IReadOnlyDictionary<string, object?> headers, CancellationToken ct) => new EventContext<TEvent> { Event = (TEvent)evt, Headers = headers, CancellationToken = ct }
            var contextType = typeof(EventContext<>).MakeGenericType(eventType);
            var evtParam = Expression.Parameter(typeof(object), "evt");
            var headersParam = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), "headers");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");
            var eventProp = contextType.GetProperty(nameof(EventContext<>.Event))!;
            var headersProp = contextType.GetProperty(nameof(EventContext<>.Headers))!;
            var ctProp = contextType.GetProperty(nameof(EventContext<>.CancellationToken))!;

            // 使用 MemberInit 表达式构建 new EventContext<T> { Event = ..., Headers = ..., CancellationToken = ... }
            var newExpr = Expression.New(contextType);
            var initExpr = Expression.MemberInit(newExpr,
                Expression.Bind(eventProp, Expression.Convert(evtParam, eventType)),
                Expression.Bind(headersProp, headersParam),
                Expression.Bind(ctProp, ctParam));
            var contextFactory = Expression.Lambda<Func<object, IReadOnlyDictionary<string, object?>, CancellationToken, object>>(Expression.Convert(initExpr, typeof(object)), evtParam, headersParam, ctParam).Compile();

            // 编译中间件调用委托: (object mw, object ctx, Func<Task> next) => ((IEventMiddleware<TEvent>)mw).HandleAsync((EventContext<TEvent>)ctx, next)
            var middlewareInterfaceType = typeof(IEventMiddleware<>).MakeGenericType(eventType);
            var handleMethod = middlewareInterfaceType.GetMethod(nameof(IEventMiddleware<>.HandleAsync)) ?? throw new MissingMethodException(middlewareInterfaceType.FullName, nameof(IEventMiddleware<>.HandleAsync));
            var mwParam = Expression.Parameter(typeof(object), "mw");
            var ctxParam = Expression.Parameter(typeof(object), "ctx");
            var nextParam = Expression.Parameter(typeof(Func<Task>), "next");
            var callExpr = Expression.Call(Expression.Convert(mwParam, middlewareInterfaceType),
                handleMethod,
                Expression.Convert(ctxParam, contextType),
                nextParam);
            var invokeDelegate = Expression.Lambda<Func<object, object, Func<Task>, Task>>(callExpr, mwParam, ctxParam, nextParam).Compile();
            return new(contextFactory, invokeDelegate);
        }
    }
}
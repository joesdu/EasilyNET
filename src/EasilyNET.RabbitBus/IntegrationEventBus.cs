using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.Abstraction;
using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Core.Attributes;
using EasilyNET.RabbitBus.Core.Enums;
using EasilyNET.RabbitBus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace EasilyNET.RabbitBus;

/// <summary>
/// RabbitMQ集成事件总线
/// </summary>
internal sealed class IntegrationEventBus : IIntegrationEventBus, IDisposable
{
    private const string HandleName = nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync);
    private readonly ISubscriptionsManager _deadLetterManager;
    private readonly ILogger<IntegrationEventBus> _logger;
    private readonly IPersistentConnection _persistentConnection;
    private readonly int _retryCount;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISubscriptionsManager _subsManager;
    private bool _isDisposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="persistentConnection"></param>
    /// <param name="logger"></param>
    /// <param name="retryCount"></param>
    /// <param name="subsManager"></param>
    /// <param name="deadLetterManager"></param>
    /// <param name="serviceProvider"></param>
    internal IntegrationEventBus(IPersistentConnection persistentConnection, ILogger<IntegrationEventBus> logger, int retryCount, ISubscriptionsManager subsManager, ISubscriptionsManager deadLetterManager, IServiceProvider serviceProvider)
    {
        _persistentConnection = persistentConnection;
        _logger = logger;
        _retryCount = retryCount;
        _subsManager = subsManager;
        _deadLetterManager = deadLetterManager;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _subsManager.Clear();
        _isDisposed = true;
    }

    /// <inheritdoc />
    public void Publish<T>(T @event, string? routingKey = null, byte? priority = 1) where T : IIntegrationEvent
    {
        if (!_persistentConnection.IsConnected) _ = _persistentConnection.TryConnect();
        var type = @event.GetType();
        _logger.LogTrace("创建通道来发布事件: {EventId} ({EventName})", @event.EventId, type.Name);
        var rabbitAttr = type.GetCustomAttribute<RabbitAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(RabbitAttribute)}>,无法创建发布事件");
        if (!rabbitAttr.Enable) return;
        var channel = _persistentConnection.CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Priority = priority.GetValueOrDefault();
        var headers = @event.GetHeaderAttributes();
        if (headers is not null && headers.Any())
        {
            properties.Headers = headers;
        }
        if (rabbitAttr is not { WorkModel: EWorkModel.None })
        {
            var exchange_args = @event.GetExchangeArgAttributes();
            channel.ExchangeDeclare(rabbitAttr.ExchangeName, rabbitAttr.WorkModel.ToDescription(), true, arguments: exchange_args);
        }
        // 创建Policy规则
        var policy = Policy.Handle<BrokerUnreachableException>()
                           .Or<SocketException>()
                           .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                               _logger.LogError(ex, "无法发布事件: {EventId} 超时 {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message));
        policy.Execute(() =>
        {
            properties.DeliveryMode = 2;
            _logger.LogTrace("发布事件: {EventId}", @event.EventId);
            channel.BasicPublish(rabbitAttr.ExchangeName, routingKey ?? rabbitAttr.RoutingKey, true, properties, JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true }));
            channel.Close();
        });
    }

    /// <inheritdoc />
    public void Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 1) where T : IIntegrationEvent
    {
        if (!_persistentConnection.IsConnected) _ = _persistentConnection.TryConnect();
        var type = @event.GetType();
        _logger.LogTrace("创建通道来发布事件: {EventId} ({EventName})", @event.EventId, type.Name);
        var rabbitAttr = type.GetCustomAttribute<RabbitAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(RabbitAttribute)}>,无法发布事件");
        if (!rabbitAttr.Enable) return;
        if (rabbitAttr is not { WorkModel: EWorkModel.Delayed }) throw new($"延时队列的交换机类型必须为{nameof(EWorkModel.Delayed)}");
        var channel = _persistentConnection.CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        //延时时间从header赋值
        var headers = @event.GetHeaderAttributes();
        if (headers is not null)
        {
            var xDelay = headers.TryGetValue("x-delay", out var delay);
            headers["x-delay"] = xDelay && ttl == 0 && delay is not null ? delay : ttl;
            properties.Headers = headers;
        }
        else
        {
            properties.Headers["x-delay"] = ttl;
        }
        properties.Priority = priority.GetValueOrDefault();
        // x-delayed-type 必须加
        var exchange_args = @event.GetExchangeArgAttributes();
        if (exchange_args is not null)
        {
            var xDelayedType = exchange_args.TryGetValue("x-delayed-type", out var delayedType);
            exchange_args["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;
        }
        ////创建延时交换机,type类型为x-delayed-message
        channel.ExchangeDeclare(rabbitAttr.ExchangeName, rabbitAttr.WorkModel.ToDescription(), true, false, exchange_args);
        // 创建Policy规则
        var policy = Policy.Handle<BrokerUnreachableException>()
                           .Or<SocketException>()
                           .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                               _logger.LogError(ex, "无法发布事件: {EventId} 超时 {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message));
        policy.Execute(() =>
        {
            _logger.LogTrace("发布事件: {EventId}", @event.EventId);
            properties.DeliveryMode = 2;
            channel.BasicPublish(rabbitAttr.ExchangeName, routingKey ?? rabbitAttr.RoutingKey, true, properties, JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true }));
            channel.Close();
        });
    }

    internal void Subscribe()
    {
        if (!_persistentConnection.IsConnected) _ = _persistentConnection.TryConnect();
        InitialRabbit();
    }

    private void InitialRabbit()
    {
        var events = AssemblyHelper.FindTypes(o => o is { IsClass: true, IsAbstract: false } && o.IsBaseOn(typeof(IntegrationEvent)) && o.HasAttribute<RabbitAttribute>());
        foreach (var eventType in events)
        {
            var rabbitAttr = eventType.GetCustomAttribute<RabbitAttribute>()!;
            if (!rabbitAttr.Enable) continue;
            _ = Task.Factory.StartNew(() =>
            {
                var xdlAttr = eventType.GetCustomAttribute<DeadLetterAttribute>();
                var eventName = _subsManager.GetEventKey(eventType);
                using var consumerChannel = CreateConsumerChannel(rabbitAttr, eventType, xdlAttr);
                if (rabbitAttr is not { WorkModel: EWorkModel.None })
                {
                    DoInternalSubscription(eventName, rabbitAttr, consumerChannel);
                }
                var handlers = AssemblyHelper.FindTypes(o => o is
                                                             {
                                                                 IsClass: true,
                                                                 IsAbstract: false
                                                             } &&
                                                             o.IsBaseOn(typeof(IIntegrationEventHandler<>))).Select(s => s.GetTypeInfo());
                var handler = handlers.FirstOrDefault(o => o.ImplementedInterfaces.Any(s => s.GenericTypeArguments.Contains(eventType)));
                using var scope = _serviceProvider.GetService<IServiceScopeFactory>()?.CreateScope();
                if (handler is not null)
                {
                    var handle = scope?.ServiceProvider.GetService(handler);
                    // 检查消费者是否已经注册,若是未注册则不启动消费.
                    if (handle is not null)
                    {
                        _subsManager.AddSubscription(eventType, handler);
                        StartBasicConsume(eventType, rabbitAttr, consumerChannel);
                    }
                }
                if (xdlAttr is null) return;
                {
                    DoInternalSubscription(eventName, xdlAttr, consumerChannel);
                    var xdl_handlers = AssemblyHelper.FindTypes(o => o is
                                                                     {
                                                                         IsClass: true,
                                                                         IsAbstract: false
                                                                     } &&
                                                                     o.IsBaseOn(typeof(IIntegrationEventDeadLetterHandler<>))).Select(s => s.GetTypeInfo());
                    var xdl_handler = xdl_handlers.FirstOrDefault(o => o.ImplementedInterfaces.Any(s => s.GenericTypeArguments.Contains(eventType)));
                    if (xdl_handler is null) return;
                    var handle = scope?.ServiceProvider.GetService(xdl_handler);
                    // 检查消费者是否已经注册,若是未注册则不启动消费.
                    if (handle is null) return;
                    _deadLetterManager.AddSubscription(eventType, xdl_handler);
                    StartBasicConsume(eventType, xdlAttr, consumerChannel);
                }
            });
        }
    }

    private IModel CreateConsumerChannel(RabbitAttribute attr, Type eventType, DeadLetterAttribute? xdlAttr = null)
    {
        _logger.LogTrace("创建消费者通道");
        var channel = _persistentConnection.CreateModel();
        var queue_args = eventType.GetQueueArgAttributes();
        if (xdlAttr is not null && xdlAttr.Enable)
        {
            queue_args ??= new Dictionary<string, object>();
            queue_args.Add("x-dead-letter-exchange", xdlAttr.ExchangeName);
            queue_args.Add("x-dead-letter-routing-key", xdlAttr.RoutingKey);
            _logger.LogTrace("创建死信消费者通道");
            var model = xdlAttr.WorkModel is EWorkModel.Delayed or EWorkModel.None ? "direct" : xdlAttr.WorkModel.ToDescription();
            //创建死信交换机
            channel.ExchangeDeclare(xdlAttr.ExchangeName, model, true);
            //创建死信队列
            _ = channel.QueueDeclare(xdlAttr.Queue, true, false, false);
        }
        if (attr is not { WorkModel: EWorkModel.None })
        {
            var exchange_args = eventType.GetExchangeArgAttributes();
            if (exchange_args is not null)
            {
                var success = exchange_args.TryGetValue("x-delayed-type", out _);
                if (!success && attr is { WorkModel: EWorkModel.Delayed }) exchange_args.Add("x-delayed-type", "direct"); //x-delayed-type必须加
            }
            //创建交换机
            channel.ExchangeDeclare(attr.ExchangeName, attr.WorkModel.ToDescription(), true, false, exchange_args);
        }
        //创建队列
        _ = channel.QueueDeclare(attr.Queue, true, false, false, queue_args);
        channel.CallbackException += (_, ea) =>
        {
            _logger.LogWarning(ea.Exception, "重新创建消费者通道");
            _subsManager.Clear();
            _deadLetterManager.Clear();
            Subscribe();
        };
        return channel;
    }

    private void DoInternalSubscription(string eventName, RabbitAttribute attr, IModel channel)
    {
        var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
        if (containsKey) return;
        if (!_persistentConnection.IsConnected) _ = _persistentConnection.TryConnect();
        channel.QueueBind(attr.Queue, attr.ExchangeName, attr.RoutingKey);
    }

    private void DoInternalSubscription(string eventName, DeadLetterAttribute attr, IModel channel)
    {
        var containsKey = _deadLetterManager.HasSubscriptionsForEvent(eventName);
        if (containsKey) return;
        if (!_persistentConnection.IsConnected) _ = _persistentConnection.TryConnect();
        channel.QueueBind(attr.Queue, attr.ExchangeName, attr.RoutingKey);
    }

    private void StartBasicConsume(Type eventType, RabbitAttribute attr, IModel? consumerChannel)
    {
        _logger.LogTrace("启动消费者");
        if (consumerChannel is not null)
        {
            var qos = eventType.GetCustomAttribute<RabbitQosAttribute>();
            if (qos is not null) consumerChannel.BasicQos(qos.PrefetchSize, qos.PrefetchCount, qos.Global);
            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            _ = consumerChannel.BasicConsume(attr.Queue, false, consumer);
            consumer.Received += async (_, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.Span);
                try
                {
                    if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new InvalidOperationException($"假异常请求:{message}");
                    }
                    await ProcessEvent(eventType, message, () => consumerChannel.BasicAck(ea.DeliveryTag, false));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "错误处理消息:{Message}", message);
                }
                // Even on exception we take the message off the queue.
                // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
                // For more information see: https://www.rabbitmq.com/dlx.html
            };
            while (true) Thread.Sleep(100000);
        }
        _logger.LogError("当_consumerChannel为null时StartBasicConsume不能调用");
    }

    private async Task ProcessEvent(Type eventType, string message, Action ack)
    {
        var eventName = _subsManager.GetEventKey(eventType);
        _logger.LogTrace("处理事件: {EventName}", eventName);
        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            var policy = Policy.Handle<BrokerUnreachableException>()
                               .Or<SocketException>()
                               .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                                   _logger.LogError(ex, "无法消费事件: {EventName} 超时 {Timeout}s ({ExceptionMessage})", eventName, $"{time.TotalSeconds:n1}", ex.Message));
            await policy.Execute(async () =>
            {
                using var scope = _serviceProvider.GetService<IServiceScopeFactory>()?.CreateScope();
                var subscriptionTypes = _subsManager.GetHandlersForEvent(eventName);
                foreach (var subscriptionType in subscriptionTypes)
                {
                    var integrationEvent = JsonSerializer.Deserialize(message, eventType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    if (integrationEvent is null)
                    {
                        throw new($"集成事件{nameof(integrationEvent)}不能为空");
                    }
                    var method = concreteType.GetMethod(HandleName);
                    if (method is null)
                    {
                        _logger.LogError($"无法找到{nameof(integrationEvent)}事件处理器下处理方法");
                        throw new($"无法找到{nameof(integrationEvent)}事件处理器下处理方法");
                    }
                    var handler = scope?.ServiceProvider.GetService(subscriptionType);
                    if (handler is null) continue;
                    await Task.Yield();
                    var obj = method.Invoke(handler, new[] { integrationEvent });
                    if (obj is null) continue;
                    await (Task)obj;
                    ack.Invoke();
                }
            });
        }
        else
        {
            _logger.LogError("没有订阅事件:{EventName}", eventName);
        }
    }

    private void StartBasicConsume(Type eventType, DeadLetterAttribute xdlAttr, IModel? channel)
    {
        _logger.LogTrace("启动死信队列消费");
        if (channel is not null)
        {
            var qos = eventType.GetCustomAttribute<RabbitQosAttribute>();
            if (qos is not null) channel.BasicQos(qos.PrefetchSize, qos.PrefetchCount, qos.Global);
            var consumer = new AsyncEventingBasicConsumer(channel);
            _ = channel.BasicConsume(xdlAttr.Queue, false, consumer);
            consumer.Received += async (_, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.Span);
                try
                {
                    if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new InvalidOperationException($"假异常请求:{message}");
                    }
                    await ProcessDeadLetterEvent(eventType, message, () => channel.BasicAck(ea.DeliveryTag, false));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "错误处理消息:{Message}", message);
                }
            };
            while (true) Thread.Sleep(100000);
        }
        _logger.LogError("当_consumerChannel为null时StartBasicConsume不能调用");
    }

    private async Task ProcessDeadLetterEvent(Type eventType, string message, Action ack)
    {
        var eventName = _deadLetterManager.GetEventKey(eventType);
        _logger.LogTrace("处理死信事件: {EventName}", eventName);
        if (_deadLetterManager.HasSubscriptionsForEvent(eventName))
        {
            var policy = Policy.Handle<BrokerUnreachableException>()
                               .Or<SocketException>()
                               .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                                   _logger.LogError(ex, "无法消费事件: {EventName} 超时 {Timeout}s ({ExceptionMessage})", eventName, $"{time.TotalSeconds:n1}", ex.Message));
            await policy.Execute(async () =>
            {
                using var scope = _serviceProvider.GetService<IServiceScopeFactory>()?.CreateScope();
                var subscriptionTypes = _deadLetterManager.GetHandlersForEvent(eventName);
                foreach (var subscriptionType in subscriptionTypes)
                {
                    var integrationEvent = JsonSerializer.Deserialize(message, eventType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var concreteType = typeof(IIntegrationEventDeadLetterHandler<>).MakeGenericType(eventType);
                    if (integrationEvent is null)
                    {
                        throw new($"集成事件{nameof(integrationEvent)}不能为空");
                    }
                    var method = concreteType.GetMethod(HandleName);
                    if (method is null)
                    {
                        _logger.LogError($"无法找到{nameof(integrationEvent)}事件处理器下处理方法");
                        throw new($"无法找到{nameof(integrationEvent)}事件处理器下处理方法");
                    }
                    var handler = scope?.ServiceProvider.GetService(subscriptionType);
                    if (handler is null) continue;
                    await Task.Yield();
                    var obj = method.Invoke(handler, new[] { integrationEvent });
                    if (obj is null) continue;
                    await (Task)obj;
                    ack.Invoke();
                }
            });
        }
        else
        {
            _logger.LogError("没有订阅死信事件:{EventName}", eventName);
        }
    }
}
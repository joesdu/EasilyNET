using EasilyNET.RabbitBus.Abstractions;
using EasilyNET.RabbitBus.Attributes;
using EasilyNET.RabbitBus.Enums;
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
    /// <param name="serviceProvider"></param>
    internal IntegrationEventBus(IPersistentConnection persistentConnection, ILogger<IntegrationEventBus> logger, int retryCount, ISubscriptionsManager subsManager, IServiceProvider serviceProvider)
    {
        _persistentConnection = persistentConnection;
        _logger = logger;
        _retryCount = retryCount;
        _subsManager = subsManager;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 释放对象
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _subsManager.Clear();
        _isDisposed = true;
    }

    /// <summary>
    /// 发布消息
    /// </summary>
    /// <typeparam name="T">消息实体</typeparam>
    /// <param name="event"></param>
    /// <param name="priority">使用优先级需要先使用RabbitArg特性为队列声明"x-max-priority"参数否则也不会生效,推荐设置1-10之间的数值</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Publish<T>(T @event, byte? priority = 1) where T : IIntegrationEvent
    {
        if (!_persistentConnection.IsConnected) _ = _persistentConnection.TryConnect();
        var type = @event.GetType();
        var policy = Policy.Handle<BrokerUnreachableException>().Or<SocketException>().WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (ex, time) => _logger.LogError(ex, "无法发布事件: {EventId} 超时 {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message));
        _logger.LogTrace("创建RabbitMQ通道来发布事件: {EventId} ({EventName})", @event.EventId, type.Name);
        var rabbitAttr = type.GetCustomAttribute<RabbitAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(RabbitAttribute)}>,无法发布事件");
        if (!rabbitAttr.Enable) return;
        if (string.IsNullOrWhiteSpace(rabbitAttr.Queue)) rabbitAttr.Queue = type.Name;
        var channel = _persistentConnection.CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Priority = priority.GetValueOrDefault();
        var headers = @event.GetHeaderAttributes();
        if (headers.Any()) properties.Headers = headers;
        var exchange_args = @event.GetExchangeArgAttributes();
        channel.ExchangeDeclare(rabbitAttr.Exchange, rabbitAttr.Type, true, arguments: exchange_args);
        policy.Execute(() =>
        {
            properties.DeliveryMode = 2;
            _logger.LogTrace("向RabbitMQ发布事件: {EventId}", @event.EventId);
            channel.BasicPublish(rabbitAttr.Exchange, rabbitAttr.RoutingKey, true, properties, JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true }));
            channel.Close();
        });
    }

    /// <summary>
    /// 基于rabbitmq_delayed_message_exchange插件实现,使用前请确认已安装好插件,发布延时队列消息,需要RabbitMQ开启延时队列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="event"></param>
    /// <param name="ttl">若是未指定ttl以及RabbitMQHeader('x-delay',uint)特性将立即消费</param>
    /// <param name="priority">使用优先级需要先使用RabbitArg特性为队列声明"x-max-priority"参数否则也不会生效,推荐设置1-10之间的数值</param>
    public void Publish<T>(T @event, uint ttl, byte? priority = 1) where T : IIntegrationEvent
    {
        if (!_persistentConnection.IsConnected) _ = _persistentConnection.TryConnect();
        var type = @event.GetType();
        var policy = Policy.Handle<BrokerUnreachableException>().Or<SocketException>().WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (ex, time) => _logger.LogError(ex, "无法发布事件: {EventId} 超时 {Timeout}s ({ExceptionMessage})", @event.EventId, $"{time.TotalSeconds:n1}", ex.Message));
        _logger.LogTrace("创建RabbitMQ通道来发布事件: {EventId} ({EventName})", @event.EventId, type.Name);
        var rabbitAttr = type.GetCustomAttribute<RabbitAttribute>() ?? throw new($"{nameof(@event)}未设置<{nameof(RabbitAttribute)}>,无法发布事件");
        if (!rabbitAttr.Enable) return;
        if (string.IsNullOrWhiteSpace(rabbitAttr.Queue)) rabbitAttr.Queue = type.Name;
        if (rabbitAttr.Type != EExchange.Delayed.ToDescription()) throw new($"延时队列的交换机类型必须为{EExchange.Delayed}");
        var channel = _persistentConnection.CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        //延时时间从header赋值
        var headers = @event.GetHeaderAttributes();
        var xDelay = headers.TryGetValue("x-delay", out var delay);
        headers["x-delay"] = xDelay && ttl == 0 && delay is not null ? delay : ttl;
        properties.Headers = headers;
        properties.Priority = priority.GetValueOrDefault();
        // x-delayed-type 必须加
        var exchange_args = @event.GetExchangeArgAttributes();
        var xDelayedType = exchange_args.TryGetValue("x-delayed-type", out var delayedType);
        exchange_args["x-delayed-type"] = !xDelayedType || delayedType is null ? "direct" : delayedType;
        ////创建延时交换机,type类型为x-delayed-message
        channel.ExchangeDeclare(rabbitAttr.Exchange, rabbitAttr.Type, true, false, exchange_args);
        policy.Execute(() =>
        {
            properties.DeliveryMode = 2;
            _logger.LogTrace("向RabbitMQ发布事件: {EventId}", @event.EventId);
            channel.BasicPublish(rabbitAttr.Exchange, rabbitAttr.RoutingKey, true, properties, JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true }));
            channel.Close();
        });
    }

    /// <summary>
    /// 集成事件订阅者处理
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    internal void Subscribe()
    {
        if (!_persistentConnection.IsConnected) _ = _persistentConnection.TryConnect();
        var handlerTypes = AssemblyHelper.FindTypes(o => o is { IsClass: true, IsAbstract: false } && o.IsBaseOn(typeof(IIntegrationEventHandler<>)));
        foreach (var handlerType in handlerTypes)
        {
            var implementedType = handlerType.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(o => o.IsBaseOn(typeof(IIntegrationEventHandler<>)));
            var eventType = implementedType?.GetTypeInfo().GenericTypeArguments.FirstOrDefault();
            if (eventType is null) continue;
            CheckEventType(eventType);
            CheckHandlerType(handlerType);
            var rabbitAttr = eventType.GetCustomAttribute<RabbitAttribute>() ?? throw new($"{nameof(eventType)}未设置<{nameof(RabbitAttribute)}>,无法发布事件");
            if (!rabbitAttr.Enable) continue;
            _ = Task.Factory.StartNew(() =>
            {
                using var consumerChannel = CreateConsumerChannel(rabbitAttr, eventType);
                var eventName = _subsManager.GetEventKey(eventType);
                DoInternalSubscription(eventName, rabbitAttr, consumerChannel);
                _subsManager.AddSubscription(eventType, handlerType);
                StartBasicConsume(eventType, rabbitAttr, consumerChannel);
            });
        }
    }

    /// <summary>
    /// 检查订阅事件是否存在
    /// </summary>
    /// <param name="eventType"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private static void CheckEventType(Type eventType)
    {
        if (!eventType.IsDeriveClassFrom<IIntegrationEvent>())
            throw new ArgumentNullException(nameof(eventType), $"{eventType}没有继承{nameof(IIntegrationEvent)}");
    }

    /// <summary>
    /// 检查订阅者是否存在
    /// </summary>
    /// <param name="handlerType"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private static void CheckHandlerType(Type handlerType)
    {
        if (!handlerType.IsBaseOn(typeof(IIntegrationEventHandler<>)))
            throw new ArgumentNullException(nameof(handlerType), $"{nameof(handlerType)}未派生自IIntegrationEventHandler<>");
    }

    private void DoInternalSubscription(string eventName, RabbitAttribute rabbitMqAttribute, IModel consumerChannel)
    {
        var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
        if (containsKey) return;
        if (!_persistentConnection.IsConnected) _ = _persistentConnection.TryConnect();
        consumerChannel.QueueBind(rabbitMqAttribute.Queue, rabbitMqAttribute.Exchange, rabbitMqAttribute.RoutingKey);
    }

    /// <summary>
    /// 创建消费者通道和队列
    /// </summary>
    /// <param name="rabbitAttr"></param>
    /// <param name="eventType"></param>
    /// <returns></returns>
    private IModel CreateConsumerChannel(RabbitAttribute rabbitAttr, Type eventType)
    {
        _logger.LogTrace("创建RabbitMQ消费者通道");
        var channel = _persistentConnection.CreateModel();
        var exchange_args = eventType.GetExchangeArgAttributes();
        var success = exchange_args.TryGetValue("x-delayed-type", out _);
        if (!success && rabbitAttr.Type == EExchange.Delayed.ToDescription()) exchange_args.Add("x-delayed-type", "direct"); //x-delayed-type必须加
        //创建交换机
        channel.ExchangeDeclare(rabbitAttr.Exchange, rabbitAttr.Type, true, false, exchange_args);
        //创建队列
        var queue_args = eventType.GetQueueArgAttributes();
        _ = channel.QueueDeclare(rabbitAttr.Queue, true, false, false, queue_args);
        channel.CallbackException += (_, ea) =>
        {
            _logger.LogWarning(ea.Exception, "重新创建RabbitMQ消费者通道");
            _subsManager.Clear();
            Subscribe();
        };
        return channel;
    }

    /// <summary>
    /// 启动消费者
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="rabbitMqAttribute"></param>
    /// <param name="consumerChannel"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private void StartBasicConsume(Type eventType, RabbitAttribute rabbitMqAttribute, IModel? consumerChannel)
    {
        _logger.LogTrace("启动RabbitMQ基本消费");
        if (consumerChannel is not null)
        {
            // 是否有必要添加限流.可以讨论.
            // consumerChannel.BasicQos(prefetchCount: 5, prefetchSize: 3, global: false);
            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            consumer.Received += async (_, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.Span);
                try
                {
                    if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase)) throw new InvalidOperationException($"假异常请求:{message}");
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
            _ = consumerChannel.BasicConsume(rabbitMqAttribute.Queue, false, consumer);
            while (true) Thread.Sleep(100000);
        }
        _logger.LogError("当_consumerChannel为null时StartBasicConsume不能调用");
    }

    /// <summary>
    /// 事件处理程序
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="message">消息</param>
    /// <param name="ack">消息消费回调</param>
    /// <returns></returns>
    private async Task ProcessEvent(Type eventType, string message, Action ack)
    {
        var eventName = _subsManager.GetEventKey(eventType);
        _logger.LogTrace("处理RabbitMQ事件: {EventName}", eventName);
        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            var policy = Policy.Handle<BrokerUnreachableException>().Or<SocketException>().WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) => _logger.LogError(ex, "无法消费事件: {EventName} 超时 {Timeout}s ({ExceptionMessage})", eventName, $"{time.TotalSeconds:n1}", ex.Message));
            await policy.Execute(async () =>
            {
                using var scope = _serviceProvider.GetService<IServiceScopeFactory>()?.CreateScope();
                var subscriptionTypes = _subsManager.GetHandlersForEvent(eventName);
                foreach (var subscriptionType in subscriptionTypes)
                {
                    var integrationEvent = JsonSerializer.Deserialize(message, eventType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    if (integrationEvent is null) throw new("集成事件不能为空");
                    var method = concreteType.GetMethod(HandleName);
                    if (method is null)
                    {
                        _logger.LogError("无法找到IIntegrationEventHandler事件处理器下处理者方法");
                        throw new("无法找到IIntegrationEventHandler事件处理器下处理者方法");
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
            _logger.LogError("没有订阅RabbitMQ事件:{EventName}", eventName);
        }
    }
}
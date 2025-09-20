using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Enums;
using RabbitMQ.Client;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.AspNetCore.Builder;

/// <summary>
///     <para xml:lang="en">Fluent builder for RabbitMQ configuration</para>
///     <para xml:lang="zh">RabbitMQ配置的流畅构建器</para>
/// </summary>
public sealed class RabbitBusBuilder
{
    private RabbitConfig Config { get; } = new();

    private EventConfigurationRegistry EventRegistry { get; } = new();

    /// <summary>
    ///     <para xml:lang="en">Configure RabbitMQ connection</para>
    ///     <para xml:lang="zh">配置RabbitMQ连接</para>
    /// </summary>
    /// <param name="configure">
    ///     <para xml:lang="en">Connection configuration action</para>
    ///     <para xml:lang="zh">连接配置操作</para>
    /// </param>
    public RabbitBusBuilder WithConnection(Action<ConnectionFactory> configure)
    {
        var factory = new ConnectionFactory();
        configure.Invoke(factory);

        // 从ConnectionFactory复制配置到RabbitConfig
        Config.Host = factory.HostName;
        Config.UserName = factory.UserName;
        Config.PassWord = factory.Password;
        Config.VirtualHost = factory.VirtualHost;
        Config.Port = factory.Port;
        Config.ConnectionString = factory.Uri;
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure consumer settings</para>
    ///     <para xml:lang="zh">配置消费者设置</para>
    /// </summary>
    /// <param name="dispatchConcurrency">
    ///     <para xml:lang="en">Consumer dispatch concurrency</para>
    ///     <para xml:lang="zh">消费者调度并发数</para>
    /// </param>
    /// <param name="prefetchCount">
    ///     <para xml:lang="en">QoS prefetch count</para>
    ///     <para xml:lang="zh">QoS预取计数</para>
    /// </param>
    /// <param name="prefetchSize">
    ///     <para xml:lang="en">QoS prefetch size</para>
    ///     <para xml:lang="zh">QoS预取大小</para>
    /// </param>
    /// <param name="global">
    ///     <para xml:lang="en">Whether QoS is global</para>
    ///     <para xml:lang="zh">QoS是否全局</para>
    /// </param>
    public RabbitBusBuilder WithConsumerSettings(ushort dispatchConcurrency = 10, ushort prefetchCount = 100, uint prefetchSize = 0, bool global = false)
    {
        Config.ConsumerDispatchConcurrency = dispatchConcurrency;
        Config.Qos.PrefetchCount = prefetchCount;
        Config.Qos.PrefetchSize = prefetchSize;
        Config.Qos.Global = global;
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure retry and resilience settings</para>
    ///     <para xml:lang="zh">配置重试和弹性设置</para>
    /// </summary>
    /// <param name="retryCount">
    ///     <para xml:lang="en">Number of retry attempts</para>
    ///     <para xml:lang="zh">重试次数</para>
    /// </param>
    /// <param name="retryIntervalSeconds">
    ///     <para xml:lang="en">The interval in seconds for the background service to check for messages to retry</para>
    ///     <para xml:lang="zh">后台服务检查要重试的消息的间隔时间（秒）</para>
    /// </param>
    /// <param name="publisherConfirms">
    ///     <para xml:lang="en">Enable publisher confirms</para>
    ///     <para xml:lang="zh">启用发布者确认</para>
    /// </param>
    /// <param name="maxOutstandingConfirms">
    ///     <para xml:lang="en">Maximum number of outstanding publisher confirms</para>
    ///     <para xml:lang="zh">最大未确认发布数量</para>
    /// </param>
    /// <param name="batchSize">
    ///     <para xml:lang="en">Batch size for batch publishing</para>
    ///     <para xml:lang="zh">批量发布大小</para>
    /// </param>
    /// <param name="confirmTimeoutMs">
    ///     <para xml:lang="en">Timeout for publisher confirms in milliseconds</para>
    ///     <para xml:lang="zh">发布确认超时时间（毫秒）</para>
    /// </param>
    public RabbitBusBuilder WithResilience(int retryCount = 5, int retryIntervalSeconds = 1, bool publisherConfirms = true, int maxOutstandingConfirms = 1000, int batchSize = 100, int confirmTimeoutMs = 30000)
    {
        Config.RetryCount = retryCount;
        Config.RetryIntervalSeconds = retryIntervalSeconds;
        Config.PublisherConfirms = publisherConfirms;
        Config.MaxOutstandingConfirms = maxOutstandingConfirms;
        Config.BatchSize = batchSize;
        Config.ConfirmTimeoutMs = confirmTimeoutMs;
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure handler concurrency settings</para>
    ///     <para xml:lang="zh">配置处理器并发设置</para>
    /// </summary>
    /// <param name="handlerMaxDegreeOfParallelism">
    ///     <para xml:lang="en">Maximum degree of parallelism for event handler execution</para>
    ///     <para xml:lang="zh">事件处理器执行的最大并行度</para>
    /// </param>
    public RabbitBusBuilder WithHandlerConcurrency(int handlerMaxDegreeOfParallelism = 4)
    {
        Config.HandlerMaxDegreeOfParallelism = handlerMaxDegreeOfParallelism;
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure application identification</para>
    ///     <para xml:lang="zh">配置应用程序标识</para>
    /// </summary>
    /// <param name="applicationName">
    ///     <para xml:lang="en">Application name</para>
    ///     <para xml:lang="zh">应用程序名称</para>
    /// </param>
    public RabbitBusBuilder WithApplication(string applicationName)
    {
        Config.ApplicationName = applicationName;
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Use custom serializer</para>
    ///     <para xml:lang="zh">使用自定义序列化器</para>
    /// </summary>
    /// <typeparam name="TSerializer">
    ///     <para xml:lang="en">Serializer type</para>
    ///     <para xml:lang="zh">序列化器类型</para>
    /// </typeparam>
    public RabbitBusBuilder WithSerializer<TSerializer>() where TSerializer : class, IBusSerializer, new()
    {
        Config.BusSerializer = new TSerializer();
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Use custom serializer instance</para>
    ///     <para xml:lang="zh">使用自定义序列化器实例</para>
    /// </summary>
    /// <param name="serializer">
    ///     <para xml:lang="en">Serializer instance</para>
    ///     <para xml:lang="zh">序列化器实例</para>
    /// </param>
    public RabbitBusBuilder WithSerializer(IBusSerializer serializer)
    {
        Config.BusSerializer = serializer;
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure exchange declaration settings</para>
    ///     <para xml:lang="zh">配置交换机声明设置</para>
    /// </summary>
    /// <param name="skipExchangeDeclare">
    ///     <para xml:lang="en">Whether to skip exchange declaration. When true, assumes exchanges already exist with correct types</para>
    ///     <para xml:lang="zh">是否跳过交换机声明。当为true时，假设交换机已存在且类型正确</para>
    /// </param>
    /// <param name="validateExchangesOnStartup">
    ///     <para xml:lang="en">Whether to validate exchange types on startup. When true, validates all configured exchanges exist with correct types</para>
    ///     <para xml:lang="zh">是否在启动时验证交换机类型。当为true时，验证所有配置的交换机是否存在且类型正确</para>
    /// </param>
    public RabbitBusBuilder WithExchangeSettings(bool skipExchangeDeclare = false, bool validateExchangesOnStartup = false)
    {
        Config.SkipExchangeDeclare = skipExchangeDeclare;
        Config.ValidateExchangesOnStartup = validateExchangesOnStartup;
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure retry queue sizing</para>
    ///     <para xml:lang="zh">配置重试队列容量</para>
    /// </summary>
    /// <param name="maxSize">
    ///     <para xml:lang="en">Fixed max size. If &gt; 0, overrides dynamic calculation</para>
    ///     <para xml:lang="zh">固定最大长度。若 &gt; 0 则覆盖动态计算</para>
    /// </param>
    /// <param name="memoryRatio">
    ///     <para xml:lang="en">Memory ratio for dynamic sizing (0-0.25). Ignored if null</para>
    ///     <para xml:lang="zh">按内存动态计算占比（0-0.25）。为 null 则不变</para>
    /// </param>
    /// <param name="avgEntryBytes">
    ///     <para xml:lang="en">Estimated average bytes per retry entry. Ignored if null</para>
    ///     <para xml:lang="zh">单条重试项估算字节。为 null 则不变</para>
    /// </param>
    public RabbitBusBuilder WithRetryQueueSizing(int? maxSize = null, double? memoryRatio = null, int? avgEntryBytes = null)
    {
        if (maxSize.HasValue)
        {
            Config.RetryQueueMaxSize = Math.Max(0, maxSize.Value);
        }
        if (memoryRatio.HasValue)
        {
            var ratio = memoryRatio.Value;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
            {
                ratio = 0.02;
            }
            Config.RetryQueueMaxMemoryRatio = Math.Clamp(ratio, 0, 0.25);
        }
        if (avgEntryBytes.HasValue)
        {
            Config.RetryQueueAvgEntryBytes = Math.Max(256, avgEntryBytes.Value);
        }
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure event with fluent API</para>
    ///     <para xml:lang="zh">使用流畅API配置事件</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="configure">
    ///     <para xml:lang="en">Event configuration action</para>
    ///     <para xml:lang="zh">事件配置操作</para>
    /// </param>
    public RabbitBusBuilder ConfigureEvent<TEvent>(Action<EventConfiguration> configure) where TEvent : IEvent
    {
        EventRegistry.Configure<TEvent>(configure);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure event with exchange settings and return configurator for chaining</para>
    ///     <para xml:lang="zh">配置带有交换机设置的事件并返回配置器以进行链式调用</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="exchangeType">
    ///     <para xml:lang="en">Exchange type</para>
    ///     <para xml:lang="zh">交换机类型</para>
    /// </param>
    /// <param name="exchangeName">
    ///     <para xml:lang="en">Exchange name</para>
    ///     <para xml:lang="zh">交换机名称</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">Routing key</para>
    ///     <para xml:lang="zh">路由键</para>
    /// </param>
    /// <param name="queueName">
    ///     <para xml:lang="en">Queue name</para>
    ///     <para xml:lang="zh">队列名称</para>
    /// </param>
    public EventConfigurator<TEvent> AddEvent<TEvent>(EModel exchangeType = EModel.None, string? exchangeName = null, string? routingKey = null, string? queueName = null) where TEvent : IEvent
    {
        EventRegistry.Configure<TEvent>(config =>
        {
            config.Exchange.Type = exchangeType;
            config.Exchange.Name = exchangeName ?? GetDefaultExchangeName(exchangeType);
            config.Exchange.RoutingKey = exchangeType switch
            {
                EModel.PublishSubscribe => string.Empty,
                EModel.None             => queueName ?? typeof(TEvent).Name, // For direct queue publishing, routing key should be queue name
                _                       => routingKey ?? typeof(TEvent).Name
            };
            config.Queue.Name = queueName ?? typeof(TEvent).Name;
            config.Enabled = true;
        });
        return new(this);
    }

    /// <summary>
    ///     <para xml:lang="en">Configure QoS for specific event</para>
    ///     <para xml:lang="zh">为特定事件配置QoS</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="prefetchCount">
    ///     <para xml:lang="en">Prefetch count</para>
    ///     <para xml:lang="zh">预取数量</para>
    /// </param>
    /// <param name="prefetchSize">
    ///     <para xml:lang="en">Prefetch size</para>
    ///     <para xml:lang="zh">预取大小</para>
    /// </param>
    /// <param name="global">
    ///     <para xml:lang="en">Whether QoS is global</para>
    ///     <para xml:lang="zh">QoS是否全局</para>
    /// </param>
    public RabbitBusBuilder WithEventQos<TEvent>(ushort prefetchCount = 1, uint prefetchSize = 0, bool global = false) where TEvent : IEvent
    {
        EventRegistry.Configure<TEvent>(config =>
        {
            config.Qos.PrefetchCount = prefetchCount;
            config.Qos.PrefetchSize = prefetchSize;
            config.Qos.Global = global;
        });
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Add headers for specific event</para>
    ///     <para xml:lang="zh">为特定事件添加头部</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="headers">
    ///     <para xml:lang="en">Headers dictionary</para>
    ///     <para xml:lang="zh">头部字典</para>
    /// </param>
    public RabbitBusBuilder WithEventHeaders<TEvent>(Dictionary<string, object?> headers) where TEvent : IEvent
    {
        EventRegistry.Configure<TEvent>(config =>
        {
            foreach (var (key, value) in headers)
            {
                config.Headers[key] = value;
            }
        });
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Add exchange arguments for specific event</para>
    ///     <para xml:lang="zh">为特定事件添加交换机参数</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="arguments">
    ///     <para xml:lang="en">Exchange arguments</para>
    ///     <para xml:lang="zh">交换机参数</para>
    /// </param>
    public RabbitBusBuilder WithEventExchangeArgs<TEvent>(Dictionary<string, object?> arguments) where TEvent : IEvent
    {
        EventRegistry.Configure<TEvent>(config =>
        {
            foreach (var (key, value) in arguments)
            {
                config.Exchange.Arguments[key] = value;
            }
        });
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Add queue arguments for specific event</para>
    ///     <para xml:lang="zh">为特定事件添加队列参数</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="arguments">
    ///     <para xml:lang="en">Queue arguments</para>
    ///     <para xml:lang="zh">队列参数</para>
    /// </param>
    public RabbitBusBuilder WithEventQueueArgs<TEvent>(Dictionary<string, object?> arguments) where TEvent : IEvent
    {
        EventRegistry.Configure<TEvent>(config =>
        {
            foreach (var (key, value) in arguments)
            {
                config.Queue.Arguments[key] = value;
            }
        });
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure handler thread count for specific event</para>
    ///     <para xml:lang="zh">为特定事件配置处理器线程数</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="threadCount">
    ///     <para xml:lang="en">Number of threads to use for processing handlers. 1 or less means single-threaded, greater than 1 means multi-threaded</para>
    ///     <para xml:lang="zh">用于处理处理器的线程数。1或小于1表示单线程，大于1表示多线程</para>
    /// </param>
    public RabbitBusBuilder WithEventHandlerThreadCount<TEvent>(int threadCount) where TEvent : IEvent
    {
        EventRegistry.Configure<TEvent>(config => config.HandlerThreadCount = threadCount);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Ignore specific handler for an event</para>
    ///     <para xml:lang="zh">忽略特定事件的处理器</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <typeparam name="THandler">
    ///     <para xml:lang="en">Handler type to ignore</para>
    ///     <para xml:lang="zh">要忽略的处理器类型</para>
    /// </typeparam>
    public RabbitBusBuilder IgnoreHandler<TEvent, THandler>() where TEvent : IEvent where THandler : IEventHandler<TEvent>
    {
        EventRegistry.Configure<TEvent>(config => config.IgnoredHandlers.Add(typeof(THandler)));
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Build the configuration and registry</para>
    ///     <para xml:lang="zh">构建配置和注册器</para>
    /// </summary>
    public (RabbitConfig Config, EventConfigurationRegistry Registry) Build() => (Config, EventRegistry);

    private static string GetDefaultExchangeName(EModel exchangeType) =>
        exchangeType switch
        {
            EModel.PublishSubscribe => "amq.fanout",
            EModel.Routing          => "amq.direct",
            EModel.Topics           => "amq.topic",
            EModel.Delayed          => "amq.delayed",
            EModel.None             => "",
            _                       => throw new ArgumentOutOfRangeException(nameof(exchangeType), exchangeType, null)
        };

    /// <summary>
    ///     <para xml:lang="en">Generic event configurator for fluent API chaining</para>
    ///     <para xml:lang="zh">泛型事件配置器，用于流畅API链式调用</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    public sealed class EventConfigurator<TEvent> where TEvent : IEvent
    {
        private readonly RabbitBusBuilder _builder;

        internal EventConfigurator(RabbitBusBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>
        ///     <para xml:lang="en">Configure QoS for the event</para>
        ///     <para xml:lang="zh">为事件配置QoS</para>
        /// </summary>
        /// <param name="prefetchCount">
        ///     <para xml:lang="en">Prefetch count</para>
        ///     <para xml:lang="zh">预取数量</para>
        /// </param>
        /// <param name="prefetchSize">
        ///     <para xml:lang="en">Prefetch size</para>
        ///     <para xml:lang="zh">预取大小</para>
        /// </param>
        /// <param name="global">
        ///     <para xml:lang="en">Whether QoS is global</para>
        ///     <para xml:lang="zh">QoS是否全局</para>
        /// </param>
        public EventConfigurator<TEvent> WithEventQos(ushort prefetchCount = 1, uint prefetchSize = 0, bool global = false)
        {
            _builder.EventRegistry.Configure<TEvent>(config =>
            {
                config.Qos.PrefetchCount = prefetchCount;
                config.Qos.PrefetchSize = prefetchSize;
                config.Qos.Global = global;
            });
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Add headers for the event</para>
        ///     <para xml:lang="zh">为事件添加头部</para>
        /// </summary>
        /// <param name="headers">
        ///     <para xml:lang="en">Headers dictionary</para>
        ///     <para xml:lang="zh">头部字典</para>
        /// </param>
        public EventConfigurator<TEvent> WithEventHeaders(Dictionary<string, object?> headers)
        {
            _builder.EventRegistry.Configure<TEvent>(config =>
            {
                foreach (var (key, value) in headers)
                {
                    config.Headers[key] = value;
                }
            });
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Add exchange arguments for the event</para>
        ///     <para xml:lang="zh">为事件添加交换机参数</para>
        /// </summary>
        /// <param name="arguments">
        ///     <para xml:lang="en">Exchange arguments</para>
        ///     <para xml:lang="zh">交换机参数</para>
        /// </param>
        public EventConfigurator<TEvent> WithEventExchangeArgs(Dictionary<string, object?> arguments)
        {
            _builder.EventRegistry.Configure<TEvent>(config =>
            {
                foreach (var (key, value) in arguments)
                {
                    config.Exchange.Arguments[key] = value;
                }
            });
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Add queue arguments for the event</para>
        ///     <para xml:lang="zh">为事件添加队列参数</para>
        /// </summary>
        /// <param name="arguments">
        ///     <para xml:lang="en">Queue arguments</para>
        ///     <para xml:lang="zh">队列参数</para>
        /// </param>
        public EventConfigurator<TEvent> WithEventQueueArgs(Dictionary<string, object?> arguments)
        {
            _builder.EventRegistry.Configure<TEvent>(config =>
            {
                foreach (var (key, value) in arguments)
                {
                    config.Queue.Arguments[key] = value;
                }
            });
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Configure the event with custom settings</para>
        ///     <para xml:lang="zh">使用自定义设置配置事件</para>
        /// </summary>
        /// <param name="configure">
        ///     <para xml:lang="en">Event configuration action</para>
        ///     <para xml:lang="zh">事件配置操作</para>
        /// </param>
        public EventConfigurator<TEvent> ConfigureEvent(Action<EventConfiguration> configure)
        {
            _builder.EventRegistry.Configure<TEvent>(configure);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Return to the main builder</para>
        ///     <para xml:lang="zh">返回主构建器</para>
        /// </summary>
        public RabbitBusBuilder And() => _builder;

        /// <summary>
        ///     <para xml:lang="en">Register a specific handler for the event</para>
        ///     <para xml:lang="zh">为事件注册特定处理器</para>
        /// </summary>
        /// <typeparam name="THandler">
        ///     <para xml:lang="en">Handler type</para>
        ///     <para xml:lang="zh">处理器类型</para>
        /// </typeparam>
        public EventConfigurator<TEvent> WithHandler<THandler>() where THandler : class, IEventHandler<TEvent>
        {
            _builder.EventRegistry.Configure<TEvent>(config =>
            {
                var handlerType = typeof(THandler);
                if (!config.Handlers.Contains(handlerType) && !config.IgnoredHandlers.Contains(handlerType))
                {
                    config.Handlers.Add(handlerType);
                }
            });
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Configure handler thread count for the event</para>
        ///     <para xml:lang="zh">为事件配置处理器线程数</para>
        /// </summary>
        /// <param name="threadCount">
        ///     <para xml:lang="en">Number of threads to use for processing handlers. 1 or less means single-threaded, greater than 1 means multi-threaded</para>
        ///     <para xml:lang="zh">用于处理处理器的线程数。1或小于1表示单线程，大于1表示多线程</para>
        /// </param>
        public EventConfigurator<TEvent> WithHandlerThreadCount(int threadCount = 1)
        {
            _builder.EventRegistry.Configure<TEvent>(config => config.HandlerThreadCount = threadCount);
            return this;
        }

        /// <summary>
        ///     <para xml:lang="en">Configure exchange settings for the event</para>
        ///     <para xml:lang="zh">为事件配置交换机设置</para>
        /// </summary>
        /// <param name="skipExchangeDeclare">
        ///     <para xml:lang="en">Whether to skip exchange declaration for this event</para>
        ///     <para xml:lang="zh">是否为此事件跳过交换机声明</para>
        /// </param>
        /// <param name="validateExchangeOnStartup">
        ///     <para xml:lang="en">Whether to validate exchange type on startup for this event</para>
        ///     <para xml:lang="zh">是否在启动时为此事件验证交换机类型</para>
        /// </param>
        public EventConfigurator<TEvent> WithExchangeSettings(bool skipExchangeDeclare = false, bool validateExchangeOnStartup = false)
        {
            _builder.Config.SkipExchangeDeclare = skipExchangeDeclare;
            _builder.Config.ValidateExchangesOnStartup = validateExchangeOnStartup;
            return this;
        }
    }
}
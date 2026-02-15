using System.Net.Sockets;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore;
using EasilyNET.RabbitBus.AspNetCore.Abstractions;
using EasilyNET.RabbitBus.AspNetCore.Builder;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Health;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.AspNetCore.Metrics;
using EasilyNET.RabbitBus.AspNetCore.Services;
using EasilyNET.RabbitBus.AspNetCore.Stores;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Polly.Timeout;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">RabbitService Extension</para>
///     <para xml:lang="zh">RabbitService扩展</para>
/// </summary>
public static class RabbitServiceExtension
{
    /// <param name="services">
    ///     <para xml:lang="en">IServiceCollection</para>
    ///     <para xml:lang="zh">服务容器</para>
    /// </param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     <para xml:lang="en">Add RabbitBus Service</para>
        ///     <para xml:lang="zh">添加RabbitBus服务</para>
        /// </summary>
        /// <param name="builder">
        ///     <para xml:lang="en">RabbitBusBuilder</para>
        ///     <para xml:lang="zh">RabbitBus构建器</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">IServiceCollection</para>
        ///     <para xml:lang="zh">服务容器</para>
        /// </returns>
        public IServiceCollection AddRabbitBus(Action<RabbitBusBuilder> builder)
        {
            var busBuilder = new RabbitBusBuilder();
            builder.Invoke(busBuilder);
            var (config, eventRegistry) = busBuilder.Build();
            RabbitBusMetrics.SetAppName(config.ApplicationName);
            services.RabbitPersistentConnection(o =>
            {
                o.ConnectionString = config.ConnectionString;
                o.Host = config.Host;
                o.UserName = config.UserName;
                o.PassWord = config.PassWord;
                o.VirtualHost = config.VirtualHost;
                o.Port = config.Port;
                o.RetryCount = config.RetryCount;
                o.PublisherConfirms = config.PublisherConfirms;
                o.MaxOutstandingConfirms = config.MaxOutstandingConfirms;
                o.BatchSize = config.BatchSize;
                o.ConfirmTimeoutMs = config.ConfirmTimeoutMs;
                o.ConsumerDispatchConcurrency = config.ConsumerDispatchConcurrency;
                o.Qos.PrefetchCount = config.Qos.PrefetchCount;
                o.Qos.PrefetchSize = config.Qos.PrefetchSize;
                o.Qos.Global = config.Qos.Global;
                o.ApplicationName = config.ApplicationName;
                o.BusSerializer = config.BusSerializer;
                o.SkipExchangeDeclare = config.SkipExchangeDeclare;
                o.ValidateExchangesOnStartup = config.ValidateExchangesOnStartup;
            });
            services.AddSingleton(eventRegistry);
            services.InjectConfiguredHandlers(eventRegistry);
            services.AddSingleton(config.BusSerializer);
            services.AddSingleton<EventPublisher>();
            services.AddSingleton(sp => new EventHandlerInvoker(sp,
                sp.GetRequiredService<IBusSerializer>(),
                sp.GetRequiredService<ILogger<EventBus>>(),
                sp.GetRequiredService<ResiliencePipelineProvider<string>>(),
                eventRegistry,
                sp.GetRequiredService<IDeadLetterStore>(),
                sp.GetRequiredService<IOptionsMonitor<RabbitConfig>>()));
            services.AddSingleton<ConsumerManager>();
            services.AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();
            services.AddSingleton<IBus, EventBus>();
            services.AddHostedService<SubscribeService>();
            services.AddHostedService<MessageConfirmService>();
            services.AddHealthChecks().AddRabbitBusHealthCheck();
            return services;
        }

        private void InjectConfiguredHandlers(EventConfigurationRegistry registry)
        {
            var configurations = registry.GetAllConfigurations().Where(c => c.Enabled);
            foreach (var config in configurations)
            {
                // Register middleware if configured
                if (config.MiddlewareType is not null)
                {
                    services.AddScoped(config.MiddlewareType);
                }
                // Register fallback handler if configured
                if (config.FallbackHandlerType is not null)
                {
                    services.AddScoped(config.FallbackHandlerType);
                }
                // Register handlers as Scoped (supports DbContext and other scoped services)
                // Prefer OrderedHandlers if populated, otherwise fall back to legacy Handlers list
                var handlerTypes = config.OrderedHandlers.Count > 0
                                       ? config.OrderedHandlers.Select(h => h.HandlerType)
                                       : config.Handlers.AsEnumerable();
                foreach (var ht in handlerTypes.Where(h => !config.IgnoredHandlers.Contains(h)).Distinct())
                {
                    services.AddScoped(ht);
                }
            }
        }

        private void RabbitPersistentConnection(Action<RabbitConfig> options)
        {
            services.Configure(Constant.OptionName, options);
            var config = new RabbitConfig();
            options.Invoke(config);
            services.AddSingleton<IConnectionFactory, ConnectionFactory>(_ =>
            {
                var factory = config.ConnectionString is not null
                                  ? new()
                                  {
                                      Uri = config.ConnectionString
                                  }
                                  : (ConnectionFactory)(config.AmqpTcpEndpoints?.Count > 0
                                                            ? new()
                                                            {
                                                                UserName = config.UserName,
                                                                Password = config.PassWord,
                                                                VirtualHost = config.VirtualHost,
                                                                ClientProvidedName = config.ApplicationName
                                                            }
                                                            : config.Host.IsNotNullOrWhiteSpace()
                                                                ? new()
                                                                {
                                                                    HostName = config.Host,
                                                                    UserName = config.UserName,
                                                                    Password = config.PassWord,
                                                                    Port = config.Port,
                                                                    VirtualHost = config.VirtualHost,
                                                                    ClientProvidedName = config.ApplicationName
                                                                }
                                                                : throw new InvalidOperationException("Configuration error: Unable to create a connection from the provided configuration."));
                factory.ConsumerDispatchConcurrency = config.ConsumerDispatchConcurrency;
                const double HeartbeatIntervalSeconds = 10d;
                factory.RequestedHeartbeat = TimeSpan.FromSeconds(HeartbeatIntervalSeconds);
                factory.ContinuationTimeout = TimeSpan.FromSeconds(HeartbeatIntervalSeconds * 2);
                return factory;
            });

            // Publishing pipeline for message publishing operations
            services.AddResiliencePipeline(Constant.PublishPipelineName, (builder, context) =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<PersistentConnection>>();
                builder.AddRetry(new()
                {
                    ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>().Handle<SocketException>().Handle<TimeoutRejectedException>(),
                    MaxRetryAttempts = config.RetryCount,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    MaxDelay = TimeSpan.FromSeconds(10),
                    OnRetry = args =>
                    {
                        var ex = args.Outcome.Exception!;
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning(ex, "RabbitMQ client failed after a timeout of {TimeOut} seconds. Exception message: {ExceptionMessage}", args.Duration.TotalSeconds, ex.Message);
                        }
                        return ValueTask.CompletedTask;
                    }
                });
                builder.AddTimeout(TimeSpan.FromMinutes(1));
            });

            // Connection pipeline for initial connection establishment only
            services.AddResiliencePipeline(Constant.ConnectionPipelineName, (builder, context) =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<PersistentConnection>>();
                builder.AddRetry(new()
                {
                    ShouldHandle = new PredicateBuilder()
                                   .Handle<BrokerUnreachableException>()
                                   .Handle<SocketException>()
                                   .Handle<TimeoutException>()
                                   .Handle<ConnectFailureException>()
                                   .Handle<AuthenticationFailureException>(),
                    MaxRetryAttempts = Math.Min(3, config.RetryCount),
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    MaxDelay = TimeSpan.FromSeconds(5),
                    OnRetry = args =>
                    {
                        var ex = args.Outcome.Exception!;
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning(ex, "Initial RabbitMQ connection attempt failed. Exception: {ExceptionMessage}", ex.Message);
                        }
                        return ValueTask.CompletedTask;
                    }
                });
                builder.AddTimeout(TimeSpan.FromSeconds(30));
            });

            // Handler pipeline: keep minimal retries, primarily guard against transient infrastructure issues
            services.AddResiliencePipeline(Constant.HandlerPipelineName, (builder, context) =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<EventBus>>();
                builder.AddRetry(new()
                {
                    ShouldHandle = new PredicateBuilder()
                                   .Handle<BrokerUnreachableException>()
                                   .Handle<SocketException>()
                                   .Handle<TimeoutException>(),
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        var ex = args.Outcome.Exception!;
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning(ex, "Handler transient failure, retrying attempt {Attempt}", args.AttemptNumber);
                        }
                        return ValueTask.CompletedTask;
                    }
                });
                builder.AddTimeout(TimeSpan.FromSeconds(30));
            });
            services.AddSingleton<PersistentConnection>();
        }
    }
}
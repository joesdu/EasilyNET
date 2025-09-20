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
using Polly;
using Polly.Timeout;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">RabbitService Extension</para>
///     <para xml:lang="zh">RabbitService扩展</para>
/// </summary>
public static class RabbitServiceExtension
{
    /// <summary>
    ///     <para xml:lang="en">Add RabbitBus Service</para>
    ///     <para xml:lang="zh">添加RabbitBus服务</para>
    /// </summary>
    /// <param name="services">
    ///     <para xml:lang="en">IServiceCollection</para>
    ///     <para xml:lang="zh">服务容器</para>
    /// </param>
    /// <param name="builder">
    ///     <para xml:lang="en">RabbitBusBuilder</para>
    ///     <para xml:lang="zh">RabbitBus构建器</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">IServiceCollection</para>
    ///     <para xml:lang="zh">服务容器</para>
    /// </returns>
    public static IServiceCollection AddRabbitBus(this IServiceCollection services, Action<RabbitBusBuilder> builder)
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
        services.AddSingleton<EventHandlerInvoker>();
        services.AddSingleton<ConsumerManager>();
        services.AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();
        services.AddSingleton<IBus, EventBus>();
        services.AddHostedService<SubscribeService>();
        services.AddHostedService<MessageConfirmManager>();
        services.AddHealthChecks().AddRabbitBusHealthCheck();
        return services;
    }

    private static void InjectConfiguredHandlers(this IServiceCollection services, EventConfigurationRegistry registry)
    {
        var handlerTypes = registry.GetAllConfigurations()
                                   .Where(c => c.Enabled)
                                   .SelectMany(c => c.Handlers.Where(h => !c.IgnoredHandlers.Contains(h)))
                                   .Distinct();
        foreach (var ht in handlerTypes)
        {
            services.AddSingleton(ht);
        }
    }

    private static void RabbitPersistentConnection(this IServiceCollection services, Action<RabbitConfig> options)
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
            factory.RequestedHeartbeat = TimeSpan.FromSeconds(30);
            return factory;
        });
        services.AddResiliencePipeline(Constant.ResiliencePipelineName, (builder, context) =>
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
        services.AddSingleton<PersistentConnection>();
    }
}
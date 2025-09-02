using System.Net.Sockets;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore;
using EasilyNET.RabbitBus.AspNetCore.Abstractions;
using EasilyNET.RabbitBus.AspNetCore.Builder;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">RabbitMQ ServiceCollection</para>
///     <para xml:lang="zh">RabbitMQ 服务集合</para>
/// </summary>
public static class RabbitServiceExtension
{
    /// <summary>
    ///     <para xml:lang="en">Adds RabbitMQ message bus service using fluent builder</para>
    ///     <para xml:lang="zh">使用流畅构建器添加RabbitMQ消息总线服务</para>
    /// </summary>
    /// <param name="services">
    ///     <para xml:lang="en">The service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    /// <param name="configure">
    ///     <para xml:lang="en">The fluent builder configuration action</para>
    ///     <para xml:lang="zh">流畅构建器配置操作</para>
    /// </param>
    public static void AddRabbitBus(this IServiceCollection services, Action<RabbitBusBuilder> configure)
    {
        var builder = new RabbitBusBuilder();
        configure(builder);
        var (config, registry) = builder.Build();
        services.RabbitPersistentConnection(options =>
        {
            // Copy configuration from builder to RabbitConfig
            options.ConnectionString = config.ConnectionString;
            options.Host = config.Host;
            options.UserName = config.UserName;
            options.PassWord = config.PassWord;
            options.VirtualHost = config.VirtualHost;
            options.Port = config.Port;
            options.RetryCount = config.RetryCount;
            options.PublisherConfirms = config.PublisherConfirms;
            options.MaxOutstandingConfirms = config.MaxOutstandingConfirms;
            options.BatchSize = config.BatchSize;
            options.ConfirmTimeoutMs = config.ConfirmTimeoutMs;
            options.ConsumerDispatchConcurrency = config.ConsumerDispatchConcurrency;
            options.Qos.PrefetchCount = config.Qos.PrefetchCount;
            options.Qos.PrefetchSize = config.Qos.PrefetchSize;
            options.Qos.Global = config.Qos.Global;
            options.ApplicationName = config.ApplicationName;
            options.BusSerializer = config.BusSerializer;
        }).AddEventBus();

        // Register the event configuration registry
        services.AddSingleton(registry);
    }

    private static void InjectHandler(this IServiceCollection services)
    {
        var handlers = AssemblyHelper.FindTypes(o =>
            o is
            {
                IsClass: true,
                IsAbstract: false
            } &&
            o.IsBaseOn(typeof(IEventHandler<>)));
        foreach (var handler in handlers)
        {
            services.AddSingleton(handler);
        }
    }

    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection services, Action<RabbitConfig> options)
    {
        services.Configure(Constant.OptionName, options);
        services.AddSingleton<IConnectionFactory, ConnectionFactory>(sp =>
        {
            var config = sp.GetRequiredService<IOptionsMonitor<RabbitConfig>>().Get(Constant.OptionName);
            return CreateConnectionFactory(config);
        });
        services.AddResiliencePipeline(Constant.ResiliencePipelineName, (builder, context) =>
        {
            var config = context.ServiceProvider.GetRequiredService<IOptionsMonitor<RabbitConfig>>().Get(Constant.OptionName);
            var logger = context.ServiceProvider.GetRequiredService<ILogger<PersistentConnection>>();
            builder.AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>().Handle<SocketException>().Handle<TimeoutRejectedException>(),
                MaxRetryAttempts = config.RetryCount,
                Delay = TimeSpan.FromMilliseconds(500),
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
        return services;
    }

    private static ConnectionFactory CreateConnectionFactory(RabbitConfig config)
    {
        var factory = config.ConnectionString is not null
                          ? new() { Uri = config.ConnectionString }
                          : (ConnectionFactory)(config.AmqpTcpEndpoints?.Count > 0
                                                    ? new() { UserName = config.UserName, Password = config.PassWord, VirtualHost = config.VirtualHost }
                                                    : config.Host.IsNotNullOrWhiteSpace()
                                                        ? new()
                                                        {
                                                            HostName = config.Host,
                                                            UserName = config.UserName,
                                                            Password = config.PassWord,
                                                            Port = config.Port,
                                                            VirtualHost = config.VirtualHost
                                                        }
                                                        : throw new InvalidOperationException("Configuration error: Unable to create a connection from the provided configuration."));
        factory.ConsumerDispatchConcurrency = config.ConsumerDispatchConcurrency;
        return factory;
    }

    private static void AddEventBus(this IServiceCollection services)
    {
        services.InjectHandler();
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptionsMonitor<RabbitConfig>>().Get(Constant.OptionName);
            return config.BusSerializer;
        });
        services.AddSingleton<ISubscriptionsManager, SubscriptionsManager>();
        services.AddSingleton<IBus, EventBus>();
        services.AddHostedService<SubscribeService>();
    }
}
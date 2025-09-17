using System.Net.Sockets;
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore;
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
        // 先注册配置注册表
        services.AddSingleton(registry);
        // 仅注册被显式配置的处理器
        services.InjectConfiguredHandlers(registry);
        // 序列化器
        services.AddSingleton(sp => sp.GetRequiredService<IOptionsMonitor<RabbitConfig>>().Get(Constant.OptionName).BusSerializer);
        services.AddSingleton<CacheManager>();
        services.AddSingleton<ConsumerManager>();
        services.AddSingleton<EventPublisher>();
        services.AddSingleton<EventHandlerInvoker>();
        services.AddSingleton<MessageConfirmManager>();
        services.AddSingleton<IBus, EventBus>();
        services.AddHostedService<SubscribeService>();
    }

    private static void InjectConfiguredHandlers(this IServiceCollection services, EventConfigurationRegistry registry)
    {
        var handlerTypes = registry.GetAllConfigurations()
                                   .Where(c => c.Enabled)
                                   .SelectMany(c => c.Handlers.Where(h => !c.IgnoredHandlers.Contains(h)))
                                   .Distinct();
        foreach (var ht in handlerTypes)
        {
            // 若外部已注册可跳过, 这里默认单例(根据你原有逻辑)
            services.AddSingleton(ht);
        }
    }

    private static void RabbitPersistentConnection(this IServiceCollection services, Action<RabbitConfig> options)
    {
        services.Configure(Constant.OptionName, options);
        services.AddSingleton<IConnectionFactory, ConnectionFactory>(sp =>
        {
            var config = sp.GetRequiredService<IOptionsMonitor<RabbitConfig>>().Get(Constant.OptionName);
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
                                                            VirtualHost = config.VirtualHost
                                                        }
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
    }
}
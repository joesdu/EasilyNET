using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

// ReSharper disable UnusedMember.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// ServiceCollection扩展
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// 添加消息总线RabbitMQ服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="action"></param>
    public static void AddRabbitBus(this IServiceCollection services, Action<RabbitConfig>? action = null) => services.RabbitPersistentConnection(config => action?.Invoke(config)).AddEventBus();

    /// <summary>
    /// 添加消息总线RabbitMQ服务(单节点模式)
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration">IConfiguration,从json配置ConnectionString.Rabbit中获取链接若是不存在则从系统环境变量中获取CONNECTIONSTRINGS_RABBIT</param>
    /// <param name="action"></param>
    public static void AddRabbitBus(this IServiceCollection services, IConfiguration configuration, Action<RabbitConfig>? action = null)
    {
        var connStr = configuration.GetConnectionString("Rabbit") ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS_RABBIT");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            throw new("💔: appsettings.json中无ConnectionStrings.Rabbit配置或环境变量中不存在CONNECTIONSTRINGS_RABBIT");
        }
        services.RabbitPersistentConnection(options =>
        {
            action?.Invoke(options);
            options.ConnectionString = connStr;
        }).AddEventBus();
    }

    private static void InjectHandler(this IServiceCollection services)
    {
        var handlers = AssemblyHelper.FindTypes(o =>
            o is
            {
                IsClass: true,
                IsAbstract: false
            } &&
            o.IsBaseOn(typeof(IEventHandler<>)) &&
            !o.HasAttribute<IgnoreHandlerAttribute>());
        foreach (var handler in handlers) services.AddSingleton(handler);
    }

    [SuppressMessage("Style", "IDE0046:转换为条件表达式", Justification = "<挂起>")]
    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection services, Action<RabbitConfig> options)
    {
        services.Configure(Constant.OptionName, options);
        services.AddSingleton<IConnectionFactory, ConnectionFactory>(sp =>
        {
            var conf = sp.GetRequiredService<IOptionsMonitor<RabbitConfig>>();
            var config = conf.Get(Constant.OptionName);
            if (config.ConnectionString is not null && !string.IsNullOrWhiteSpace(config.ConnectionString))
            {
                return new()
                {
                    Uri = new(config.ConnectionString),
                    DispatchConsumersAsync = true
                };
            }
            if (config.AmqpTcpEndpoints is not null && config.AmqpTcpEndpoints.Count is not 0)
            {
                return new()
                {
                    UserName = config.UserName,
                    Password = config.PassWord,
                    VirtualHost = config.VirtualHost,
                    DispatchConsumersAsync = true
                };
            }
            if (config.Host is not null && !string.IsNullOrWhiteSpace(config.Host))
            {
                return new()
                {
                    HostName = config.Host,
                    UserName = config.UserName,
                    Password = config.PassWord,
                    Port = config.Port,
                    VirtualHost = config.VirtualHost,
                    DispatchConsumersAsync = true
                };
            }
            throw new("无法从配置中创建链接");
        });
        services.AddResiliencePipeline(Constant.ResiliencePipelineName, (builder, context) =>
        {
            var conf = context.ServiceProvider.GetRequiredService<IOptionsMonitor<RabbitConfig>>();
            var config = conf.Get(Constant.OptionName);
            var logger = context.ServiceProvider.GetRequiredService<ILogger<IPersistentConnection>>();
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
                    logger.LogWarning(ex, "RabbitMQ客户端在 {TimeOut}s 超时后失败,({ExceptionMessage})", $"{args.Duration:n1}", ex.Message);
                    return ValueTask.CompletedTask;
                }
            });
            builder.AddTimeout(TimeSpan.FromMinutes(1));
        });
        services.AddSingleton<IPersistentConnection, PersistentConnection>();
        return services;
    }

    private static void AddEventBus(this IServiceCollection services)
    {
        services.InjectHandler();
        services.AddSingleton<IBusSerializerFactory, BusSerializerFactory>();
        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IBusSerializerFactory>();
            return factory.CreateSerializer();
        });
        services.AddSingleton<ISubscriptionsManager, SubscriptionsManager>();
        services.AddSingleton<IBus, EventBus>();
        services.AddHostedService<SubscribeService>();
    }
}
using System.Net.Sockets;
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

// ReSharper disable UnusedMember.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// RabbitMQ ServiceCollection
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
    /// 添加消息总线RabbitMQ服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="action"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void AddRabbitBus(this IServiceCollection services, IConfiguration configuration, Action<RabbitConfig>? action = null)
    {
        var connStr = configuration.GetConnectionString("Rabbit") ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS_RABBIT");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            throw new InvalidOperationException("Configuration error: Missing 'ConnectionStrings.Rabbit' in appsettings.json or 'CONNECTIONSTRINGS_RABBIT' environment variable.");
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
                    logger.LogWarning(ex, "RabbitMQ client failed after a timeout of {TimeOut} seconds. Exception message: {ExceptionMessage}", args.Duration.TotalSeconds, ex.Message);
                    return ValueTask.CompletedTask;
                }
            });
            builder.AddTimeout(TimeSpan.FromMinutes(1));
        });
        services.AddSingleton<IPersistentConnection, PersistentConnection>();
        return services;
    }

    private static ConnectionFactory CreateConnectionFactory(RabbitConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            return new() { Uri = new(config.ConnectionString) };
        }
        if (config.AmqpTcpEndpoints?.Count > 0)
        {
            return new() { UserName = config.UserName, Password = config.PassWord, VirtualHost = config.VirtualHost };
        }
        if (!string.IsNullOrWhiteSpace(config.Host))
        {
            return new() { HostName = config.Host, UserName = config.UserName, Password = config.PassWord, Port = config.Port, VirtualHost = config.VirtualHost };
        }
        throw new InvalidOperationException("Configuration error: Unable to create a connection from the provided configuration.");
    }

    private static void AddEventBus(this IServiceCollection services)
    {
        services.InjectHandler();
        services.AddSingleton<IBusSerializerFactory, BusSerializerFactory>();
        services.AddSingleton(sp => sp.GetRequiredService<IBusSerializerFactory>().CreateSerializer());
        services.AddSingleton<ISubscriptionsManager, SubscriptionsManager>();
        services.AddSingleton<IBus, EventBus>();
        services.AddHostedService<SubscribeService>();
    }
}
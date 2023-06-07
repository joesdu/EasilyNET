using EasilyNET.RabbitBus.Abstraction;
using EasilyNET.RabbitBus.Configs;
using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Manager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.RabbitBus;

/// <summary>
/// ServiceCollection扩展
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// 添加消息总线RabbitMQ服务(集群模式)
    /// </summary>
    /// <param name="service"></param>
    /// <param name="action"></param>
    public static void AddRabbitBus(this IServiceCollection service, Action<RabbitMultiConfig>? action = null)
    {
        RabbitMultiConfig config = new();
        action?.Invoke(config);
        service.RabbitPersistentConnection(config).AddIntegrationEventBus(config.RetryCount);
    }

    /// <summary>
    /// 添加消息总线RabbitMQ服务(单节点模式)
    /// </summary>
    /// <param name="service"></param>
    /// <param name="action"></param>
    public static void AddRabbitBus(this IServiceCollection service, Action<RabbitSingleConfig>? action = null)
    {
        RabbitSingleConfig config = new();
        action?.Invoke(config);
        service.RabbitPersistentConnection(config).AddIntegrationEventBus(config.RetryCount);
    }

    /// <summary>
    /// 添加消息总线RabbitMQ服务(单节点模式)
    /// </summary>
    /// <param name="service"></param>
    /// <param name="config">IConfiguration,从json配置ConnectionString.Rabbit中获取链接</param>
    /// <param name="retry_count">重试次数</param>
    /// <param name="max_channel_count">最大Channel池数量,默认为: 计算机上逻辑处理器的数量</param>
    public static void AddRabbitBus(this IServiceCollection service, IConfiguration config, int retry_count = 5, uint max_channel_count = 0)
    {
        var conn = config.GetConnectionString("Rabbit") ?? throw new("链接字符串不能为空");
        service.AddRabbitBus(conn, retry_count, max_channel_count);
    }

    /// <summary>
    /// 添加消息总线RabbitMQ服务
    /// </summary>
    /// <param name="service"></param>
    /// <param name="conn_str">AMQP链接字符串</param>
    /// <param name="retry_count">重试次数</param>
    /// <param name="max_channel_count">最大Channel池数量,默认为: 计算机上逻辑处理器的数量</param>
    public static void AddRabbitBus(this IServiceCollection service, string conn_str, int retry_count = 5, uint max_channel_count = 0)
    {
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(conn_str, nameof(conn_str));
#else
        if (string.IsNullOrWhiteSpace(conn_str)) throw new("链接字符串不能为空");
#endif
        service.RabbitPersistentConnection(conn_str, retry_count, max_channel_count).AddIntegrationEventBus(retry_count);
    }

    private static void AddIntegrationEventBus(this IServiceCollection service, int retry_count)
    {
        service.AddSingleton<IIntegrationEventBus, IntegrationEventBus>(sp =>
               {
                   var rabbitConn = sp.GetRequiredService<IPersistentConnection>();
                   var logger = sp.GetRequiredService<ILogger<IntegrationEventBus>>();
                   var subsManager = sp.GetRequiredService<SubscriptionsManager>();
                   var deadLetterManager = sp.GetRequiredService<DeadLetterSubscriptionsManager>();
                   return rabbitConn is null
                              ? throw new(nameof(rabbitConn))
                              : new IntegrationEventBus(rabbitConn, logger, retry_count, subsManager, deadLetterManager, sp);
               })
               .AddSingleton<SubscriptionsManager>()
               .AddSingleton<DeadLetterSubscriptionsManager>()
               .AddHostedService<SubscribeService>();
    }

    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection service, RabbitSingleConfig config)
    {
        _ = service.AddSingleton<IPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PersistentConnection>>();
            return new PersistentConnection(new ConnectionFactory
            {
                HostName = config.Host,
                UserName = config.UserName,
                Password = config.PassWord,
                Port = config.Port,
                VirtualHost = config.VirtualHost,
                DispatchConsumersAsync = true
            }, logger, config.RetryCount, config.MaxChannelPoolCount);
        });
        return service;
    }

    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection service, RabbitMultiConfig config)
    {
        if (config.AmqpTcpEndpoints is null || config.AmqpTcpEndpoints.Count is 0)
            throw new ArgumentNullException($"{nameof(config.AmqpTcpEndpoints)}不能为空", nameof(config.AmqpTcpEndpoints));
        _ = service.AddSingleton<IPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PersistentConnection>>();
            return new PersistentConnection(new ConnectionFactory
            {
                UserName = config.UserName,
                Password = config.PassWord,
                VirtualHost = config.VirtualHost,
                DispatchConsumersAsync = true
            }, logger, config.RetryCount, config.MaxChannelPoolCount, config.AmqpTcpEndpoints);
        });
        return service;
    }

    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection service, string conn, int retry_count, uint maxChannel)
    {
        _ = service.AddSingleton<IPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PersistentConnection>>();
            return new PersistentConnection(new ConnectionFactory
            {
                Uri = new(conn),
                DispatchConsumersAsync = true
            }, logger, retry_count, maxChannel);
        });
        return service;
    }
}
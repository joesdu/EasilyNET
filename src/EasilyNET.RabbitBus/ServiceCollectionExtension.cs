using EasilyNET.RabbitBus.Abstraction;
using EasilyNET.RabbitBus.Core;
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
    /// 添加消息总线RabbitMQ服务
    /// </summary>
    /// <param name="service"></param>
    /// <param name="action"></param>
    public static void AddRabbitBus(this IServiceCollection service, Action<RabbitConfig>? action = null)
    {
        RabbitConfig config = new();
        action?.Invoke(config);
        service.RabbitPersistentConnection(config).AddIntegrationEventBus(config.RetryCount);
    }

    /// <summary>
    /// 添加消息总线RabbitMQ服务
    /// </summary>
    /// <param name="service"></param>
    /// <param name="config">IConfiguration</param>
    /// <param name="retry_count">重试次数</param>
    public static void AddRabbitBus(this IServiceCollection service, IConfiguration config, int retry_count = 5)
    {
        var conn = config.GetConnectionString("Rabbit") ?? throw new("链接字符串不能为空");
        service.AddRabbitBus(conn, retry_count);
    }

    /// <summary>
    /// 添加消息总线RabbitMQ服务
    /// </summary>
    /// <param name="service"></param>
    /// <param name="conn_str">AMQP链接字符串</param>
    /// <param name="retry_count">重试次数</param>
    public static void AddRabbitBus(this IServiceCollection service, string conn_str, int retry_count = 5)
    {
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(conn_str,nameof(conn_str));
#else
        if (string.IsNullOrWhiteSpace(conn_str)) throw new("链接字符串不能为空");
#endif
        service.RabbitPersistentConnection(conn_str, retry_count).AddIntegrationEventBus(retry_count);
    }

    private static void AddIntegrationEventBus(this IServiceCollection service, int retry_count)
    {
        service.AddSingleton<IIntegrationEventBus, IntegrationEventBus>(sp =>
               {
                   var rabbitConn = sp.GetRequiredService<IPersistentConnection>();
                   var logger = sp.GetRequiredService<ILogger<IntegrationEventBus>>();
                   var subsManager = sp.GetRequiredService<ISubscriptionsManager>();
                   return rabbitConn is null
                              ? throw new(nameof(rabbitConn))
                              : new IntegrationEventBus(rabbitConn, logger, retry_count, subsManager, sp);
               })
               .AddSingleton<ISubscriptionsManager, SubscriptionsManager>().AddHostedService<SubscribeService>();
    }

    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection service, RabbitConfig config)
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
            }, logger, config.RetryCount);
        });
        return service;
    }

    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection service, string conn, int retry_count)
    {
        _ = service.AddSingleton<IPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PersistentConnection>>();
            return new PersistentConnection(new ConnectionFactory
            {
                Uri = new(conn),
                DispatchConsumersAsync = true
            }, logger, retry_count);
        });
        return service;
    }
}
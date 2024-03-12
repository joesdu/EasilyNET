using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Microsoft.Extensions.DependencyInjection;

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
        service.RabbitPersistentConnection(config).AddEventBus(config.RetryCount);
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
        service.RabbitPersistentConnection(config).AddEventBus(config.RetryCount);
    }

    /// <summary>
    /// 添加消息总线RabbitMQ服务(单节点模式)
    /// </summary>
    /// <param name="service"></param>
    /// <param name="config">IConfiguration,从json配置ConnectionString.Rabbit中获取链接</param>
    /// <param name="retry">重试次数</param>
    /// <param name="poolCount">Channel池数量,默认为: 计算机上逻辑处理器的数量</param>
    public static void AddRabbitBus(this IServiceCollection service, IConfiguration config, int retry = 5, uint poolCount = 0)
    {
        var conn = config.GetConnectionString("Rabbit") ?? throw new("链接字符串不能为空");
        service.AddRabbitBus(conn, retry, poolCount);
    }

    /// <summary>
    /// 添加消息总线RabbitMQ服务
    /// </summary>
    /// <param name="service"></param>
    /// <param name="conn">AMQP链接字符串</param>
    /// <param name="retry">重试次数</param>
    /// <param name="poolCount">Channel池数量,默认为: 计算机上逻辑处理器的数量</param>
    public static void AddRabbitBus(this IServiceCollection service, string conn, int retry = 5, uint poolCount = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(conn, nameof(conn));
        service.RabbitPersistentConnection(conn, retry, poolCount).AddEventBus(retry);
    }

    private static void InjectHandler(this IServiceCollection service)
    {
        var handlers = AssemblyHelper.FindTypes(o => o is
                                                     {
                                                         IsClass: true,
                                                         IsAbstract: false
                                                     } &&
                                                     o.IsBaseOn(typeof(IEventHandler<>)) &&
                                                     !o.HasAttribute<IgnoreHandlerAttribute>());
        foreach (var handler in handlers) service.AddSingleton(handler);
    }

    private static void AddEventBus(this IServiceCollection service, int retry)
    {
        service.InjectHandler();
        service.AddSingleton<IBus, EventBus>(sp =>
               {
                   var rabbitConn = sp.GetRequiredService<IPersistentConnection>();
                   var logger = sp.GetRequiredService<ILogger<EventBus>>();
                   var subsManager = sp.GetRequiredService<SubscriptionsManager>();
                   return rabbitConn is null
                              ? throw new(nameof(rabbitConn))
                              : new EventBus(rabbitConn, retry, subsManager, sp, logger);
               })
               .AddSingleton<SubscriptionsManager>()
               .AddHostedService<SubscribeService>();
    }

    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection service, RabbitSingleConfig config)
    {
        service.AddSingleton<IPersistentConnection>(sp =>
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
            }, logger, config.RetryCount, config.PoolCount);
        });
        return service;
    }

    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection service, RabbitMultiConfig config)
    {
        if (config.AmqpTcpEndpoints is null || config.AmqpTcpEndpoints.Count is 0)
            throw new($"{nameof(config.AmqpTcpEndpoints)}不能为空");
        service.AddSingleton<IPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PersistentConnection>>();
            return new PersistentConnection(new ConnectionFactory
            {
                UserName = config.UserName,
                Password = config.PassWord,
                VirtualHost = config.VirtualHost,
                DispatchConsumersAsync = true
            }, logger, config.RetryCount, config.PoolCount, config.AmqpTcpEndpoints);
        });
        return service;
    }

    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection service, string conn, int retry, uint poolCount)
    {
        service.AddSingleton<IPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PersistentConnection>>();
            return new PersistentConnection(new ConnectionFactory
            {
                Uri = new(conn),
                DispatchConsumersAsync = true
            }, logger, retry, poolCount);
        });
        return service;
    }
}

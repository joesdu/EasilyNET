using EasilyNET.RabbitBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

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
        _ = service.RabbitPersistentConnection(config)
                   .AddSingleton<IIntegrationEventBus, IntegrationEventBus>(sp =>
                   {
                       var rabbitConn = sp.GetRequiredService<IPersistentConnection>();
                       var logger = sp.GetRequiredService<ILogger<IntegrationEventBus>>();
                       var subsManager = sp.GetRequiredService<ISubscriptionsManager>();
                       return rabbitConn is null
                                  ? throw new(nameof(rabbitConn))
                                  : new IntegrationEventBus(rabbitConn, logger, config.RetryCount, subsManager, sp);
                   })
                   .AddSingleton<ISubscriptionsManager, SubscriptionsManager>().AddHostedService<SubscribeService>();
    }

    /// <summary>
    /// RabbitMQ持久化链接
    /// </summary>
    /// <param name="service"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    private static IServiceCollection RabbitPersistentConnection(this IServiceCollection service, RabbitConfig config)
    {
        _ = service.AddSingleton<IPersistentConnection>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PersistentConnection>>();
            return new PersistentConnection(new ConnectionFactory
            {
                HostName = config.Host,
                DispatchConsumersAsync = true,
                UserName = config.UserName,
                Password = config.PassWord,
                Port = config.Port,
                VirtualHost = config.VirtualHost
            }, logger, config.RetryCount);
        });
        return service;
    }
}
using EasilyNET.AutoDependencyInjection.Attributes;
using EasilyNET.RabbitBus.Abstractions;
using System.Text.Json;
using WebApi.Test.Unit.Events;

namespace WebApi.Test.Unit.EventHandlers;

/// <summary>
/// 天气消息处理
/// </summary>
[DependencyInjection(ServiceLifetime.Transient, AddSelf = true)]
public class WeatherForecastHandler : IIntegrationEventHandler<WeatherForecastEvent>
{
    private readonly ILogger<WeatherForecastHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger"></param>
    public WeatherForecastHandler(ILogger<WeatherForecastHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(WeatherForecastEvent @event)
    {
        _logger.LogInformation("消息到达");
        Console.WriteLine(JsonSerializer.Serialize(@event));
        return Task.CompletedTask;
    }
}
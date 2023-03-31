using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Core.Attributes;
using EasilyNET.RabbitBus.Core.Enums;

namespace WebApi.Test.Unit.Events;

/// <summary>
/// 测试消息类型
/// </summary>
[Rabbit("rabbit.bus.test", EExchange.Routing, "test", "weather"), RabbitExchangeArg("x-max-priority", 10)]
public class WeatherForecastEvent : IntegrationEvent
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 摄氏度
    /// </summary>
    public int TemperatureC { get; set; }

    /// <summary>
    /// 华氏度
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; }
}

/// <summary>
/// 测试消息
/// </summary>
[Rabbit("rabbit.bus.test1", EExchange.Routing, "test1", "test")]
public class TestEvent : IntegrationEvent { }
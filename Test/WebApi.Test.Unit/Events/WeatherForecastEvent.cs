using EasilyNET.RabbitBus;
using EasilyNET.RabbitBus.Attributes;
using EasilyNET.RabbitBus.Enums;

namespace WebApi.Test.Unit.Events;

/// <summary>
/// 测试消息类型
/// </summary>
[Rabbit("amber.bus.test", EExchange.Routing, "test", "weather")]
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
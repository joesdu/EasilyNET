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
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}
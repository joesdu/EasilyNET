using EasilyNET.RabbitBus.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Events;

namespace WebApi.Test.Unit.Controllers;

[ApiController, Route("api/[controller]")]
public class RabbitBusController : ControllerBase
{
    private static readonly string[] Summaries = { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

    private readonly IIntegrationEventBus _ibus;

    public RabbitBusController(IIntegrationEventBus ibus)
    {
        _ibus = ibus;
    }

    [HttpPost(Name = "WeatherForecast")]
    public void Post()
    {
        var weathers = Enumerable.Range(1, 5).Select(index => new WeatherForecastEvent
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
        foreach (var weather in weathers) _ibus.Publish(weather);
        // 发送延时消息,同时交换机类型必须为 EExchange.Delayed
        //_ibus.Publish(weather, 5000);
    }
}
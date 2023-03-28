using EasilyNET.Core.Language;
using EasilyNET.RabbitBus.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Events;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 消息总线测试控制器
/// </summary>
[ApiController, Route("api/[controller]")]
public class RabbitBusController : ControllerBase
{
    private static readonly string[] Summaries = { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

    private readonly IIntegrationEventBus _ibus;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ibus"></param>
    public RabbitBusController(IIntegrationEventBus ibus)
    {
        _ibus = ibus;
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    [HttpPost]
    public void Post()
    {
        var temp = new List<List<WeatherForecastEvent>>();
        foreach (var _ in ..30)
        {
            var weathers = Enumerable.Range(1, 5000).Select(index => new WeatherForecastEvent
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToList();
            temp.Add(weathers);
        }
        foreach (var weathers in temp)
        {
            Task.Run(() =>
            {
                foreach (var weather in weathers)
                {
                    _ibus.Publish(weather);
                }
            });
        }
        // 发送延时消息,同时交换机类型必须为 EExchange.Delayed
        //_ibus.Publish(weather, 5000);
    }
}
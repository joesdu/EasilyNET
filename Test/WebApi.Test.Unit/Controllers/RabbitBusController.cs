using EasilyNET.Core.Language;
using EasilyNET.RabbitBus.Abstractions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Events;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 消息总线测试控制器
/// </summary>
[ApiController, Route("api/[controller]/[action]")]
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
        foreach (var _ in ..9)
        {
            var weathers = Enumerable.Range(1, 5000).Select(index => new WeatherForecastEvent
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToList();
            temp.Add(weathers);
        }
        var rand = new Random();
        foreach (var weathers in temp)
        {
            Task.Run(() =>
            {
                foreach (var weather in weathers)
                {
                    _ibus.Publish(weather, (byte)rand.Next(0, 9));
                }
            });
        }
        // 发送延时消息,同时交换机类型必须为 EExchange.Delayed
        //_ibus.Publish(weather, 5000);
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    [HttpPost]
    public void PostOne()
    {
        var rand = new Random();
        _ibus.Publish(new WeatherForecastEvent
        {
            Date = DateOnly.FromDateTime(DateTime.Now),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }, (byte)rand.Next(0, 9));
        _ibus.Publish(new TestEvent());
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    [HttpPost]
    public void PostOneWithOutPriority()
    {
        _ibus.Publish(new WeatherForecastEvent
        {
            Date = DateOnly.FromDateTime(DateTime.Now),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        });
    }
}
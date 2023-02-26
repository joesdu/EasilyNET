using Microsoft.AspNetCore.Mvc;
using Test.ServiceModules;

namespace Test.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = {"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"};

    private readonly ITest _test;

    public WeatherForecastController(ITest test)
    {
        _test = test;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        _test.Show();
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast {Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)), TemperatureC = Random.Shared.Next(-20, 55), Summary = Summaries[Random.Shared.Next(Summaries.Length)]})
            .ToArray();
    }
}
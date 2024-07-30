using Microsoft.AspNetCore.Mvc;

namespace WebApi.SourceGenerator.Test.Controllers;

/// <summary>
/// </summary>
/// <param name="wfs"></param>
[ApiController, Route("[controller]")]
public class WeatherForecastController(WeatherForecastService wfs) : ControllerBase
{
    /// <summary>
    /// </summary>
    /// <returns></returns>
    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get() => wfs.Get();
}
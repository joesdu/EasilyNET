using System.ComponentModel;
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
    [HttpGet("GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get() => wfs.Get();

    /// <summary>
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetEnumDescription")]
    public string GetEnumDescription() => ETestEnumDescription.A.ToDescription();
}

/// <summary>
/// 测试枚举源码生成器
/// </summary>
public enum ETestEnumDescription
{
    /// <summary>
    /// A
    /// </summary>
    [Description("ADES")]
    A,

    /// <summary>
    /// B
    /// </summary>
    [Description("BDES")]
    B,

    /// <summary>
    /// C
    /// </summary>
    [Description("CDES")]
    C
}
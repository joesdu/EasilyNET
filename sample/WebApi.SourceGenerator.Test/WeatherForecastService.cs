using EasilyNET.AutoDependencyInjection.Core.Attributes;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.SourceGenerator.Test;

/// <summary>
/// 天气服务
/// </summary>
[DependencyInjection(ServiceLifetime.Transient)]
public class WeatherForecastService
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    /// <summary>
    /// 获取天气服务
    /// </summary>
    /// <returns></returns>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }
}
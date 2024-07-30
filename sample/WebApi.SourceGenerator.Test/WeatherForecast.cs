namespace WebApi.SourceGenerator.Test;

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}
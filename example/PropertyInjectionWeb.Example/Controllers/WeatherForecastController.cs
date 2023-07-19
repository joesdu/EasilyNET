using EasilyNET.PropertyInjection.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace PropertyInjectionWeb.Example.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    [Injection]
    private readonly ITest _test;
    [Injection]
    private readonly ILogger<WeatherForecastController> _logger;
    [Injection]
    private readonly ITest1 _test1;
    
    
    //
    // public WeatherForecastController(ILogger<WeatherForecastController> logger)
    // {
    //     _logger = logger;
    // }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        _logger?.LogInformation("控制调用");
        _test?.Show();
        _test1?.Show();
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }

    [Injection]
    private readonly IUserService<User> _userService;
    
    [Injection]
    private readonly UserService<User> _userService1;
    
    [HttpPost("Add")]
    public ActionResult Add([FromBody] User user)
    {
        _userService?.Add(user);
        return new JsonResult("添加成功");
    }

    [HttpGet("GetUsers")]
    public IEnumerable<User> GetUsers()
    {
       return  _userService1?.Get();
    }
}
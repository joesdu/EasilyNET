using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace WebApi.Test.Unit.Controllers;

/// <inheritdoc />
[Route("api/[controller]")]
[ApiController]
[ApiGroup("GarnetTest", "v1", "Garnet的基本使用")]
public class GarnetController : ControllerBase
{
    private readonly IDatabase _rdb;

    /// <inheritdoc />
    public GarnetController(IConnectionMultiplexer redis)
    {
        var db = redis.GetDatabase(0);
        _rdb = db;
    }

    /// <summary>
    /// 设置数据到Garnet
    /// </summary>
    /// <returns></returns>
    [HttpPost("SetSomething")]
    public async Task SetSomething()
    {
        await _rdb.StringSetAsync("test", "Hello Garnet");
    }

    /// <summary>
    /// 从Garnet中获取数据
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetSomething")]
    public async Task<string?> GetSomething() => await _rdb.StringGetAsync("test");
}
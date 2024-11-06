using EasilyNET.Core.Attributes;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace WebApi.Test.Unit.Controllers;

/// <inheritdoc />
[Route("api/[controller]")]
[ApiController]
[ApiGroup("GarnetTest")]
public class GarnetController(IDatabase garnet) : ControllerBase
{
    /// <summary>
    /// 设置数据到Garnet
    /// </summary>
    /// <returns></returns>
    [HttpPost("SetSomething")]
    public async Task SetSomething()
    {
        await garnet.StringSetAsync("test", "Hello Garnet");
    }

    /// <summary>
    /// 从Garnet中获取数据
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetSomething")]
    public async Task<string?> GetSomething() => await garnet.StringGetAsync("test");
}
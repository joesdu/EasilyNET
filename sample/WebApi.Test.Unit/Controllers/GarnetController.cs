using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace WebApi.Test.Unit.Controllers;

/// <inheritdoc />
[Route("api/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "GarnetTest")]
public class GarnetController(IDistributedCache garnet) : ControllerBase
{
    /// <summary>
    /// 设置数据到Garnet
    /// </summary>
    /// <returns></returns>
    [HttpPost("SetSomething")]
    public async Task SetSomething()
    {
        await garnet.SetStringAsync("test", "Hello Garnet");
    }

    /// <summary>
    /// 从Garnet中获取数据
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetSomething")]
    public async Task<string?> GetSomething() => await garnet.GetStringAsync("test");
}
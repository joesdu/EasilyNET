using EasilyNET.WebCore.Attributes;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 世界级防重复请求测试接口
/// </summary>
[ApiController, Route("[controller]/[action]"), ApiGroup("RepeatSubmit", "v1", "请求提交"), RepeatSubmit(400)]
public sealed class RepeatSubmitController : ControllerBase
{
    /// <summary>
    /// Get
    /// </summary>
    [HttpGet]
    public void Get() { }

    /// <summary>
    /// Add
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    [HttpPost("Add"), RepeatSubmit]
    public async Task<User> AddUser([FromBody] User user)
    {
        await Task.CompletedTask;
        return user;
    }

    /// <summary>
    /// Update
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    [HttpPut("Update"), RepeatSubmit(4000)]
    public async Task<User> UpdateUser(User user)
    {
        await Task.CompletedTask;
        return user;
    }
}

/// <summary>
/// 测试用的实体
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="CreateTime"></param>
/// <param name="UpdateTime"></param>
/// <param name="Age"></param>
public sealed record User(Guid Id, string Name, DateTimeOffset CreateTime, DateTime UpdateTime, int Age);
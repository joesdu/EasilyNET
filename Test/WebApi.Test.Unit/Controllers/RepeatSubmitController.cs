using EasilyNET.WebCore.Filters;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

[ApiController, Route("[controller]/[action]"), ApiGroup("RepeatSubmit", "v1", "请求提交")]
public sealed class RepeatSubmitController : ControllerBase
{
    
    [HttpGet]
    public void Get()
    {
        
    }

    [HttpPost("Add")]
    [TypeFilter(typeof(RepeatSubmitFilter), Arguments = new object[] { 86400000 } )]

    public async Task<User> AddUser([FromBody] User user,string test)
    {

        await Task.CompletedTask;
        return user;
    }
    
    [HttpPut("Update")]
    [TypeFilter(typeof(RepeatSubmitFilter))]
    public async Task<User> UpdateUser(User user)
    {
        await Task.CompletedTask;
        return user;
    }
}

public sealed record User(Guid Id, string Name, DateTimeOffset CreateTime, DateTime UpdateTime,int Age);

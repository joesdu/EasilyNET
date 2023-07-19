using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <inheritdoc />
[ApiController, Route("[controller]/[action]"), ApiGroup("PropertyInjection", "v1", "属性注入")]
public class PropertyInjectionController : ControllerBase
{
    [Injection]
    private readonly ILogger<PropertyInjectionController>? _logger = null;

    [Injection]
    private readonly ITest? _test = null;

    [Injection]
    private readonly ITest1? _test1 = null;

    [Injection]
    private readonly IUserService<User>? _userService = null;

    [Injection]
    private readonly UserService<User>? _userService1 = null;

    /// <summary>
    /// </summary>
    [HttpGet]
    public void Get()
    {
        _logger?.LogInformation("控制调用");
        _test?.Show();
        _test1?.Show();
    }

    /// <summary>
    /// Add
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    [HttpPost]
    public User Add(User user)
    {
        _userService?.Add(user);
        return user;
    }

    /// <summary>
    /// GetUsers
    /// </summary>
    /// <returns></returns>
    [HttpGet("Users")]
    public IEnumerable<User>? Users() => _userService1?.Get();
}
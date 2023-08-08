using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Services.Abstraction;

namespace WebApi.Test.Unit.Controllers;

/// <inheritdoc />
[ApiController, Route("[controller]/[action]"), ApiGroup("PropertyInjection", "v1", "属性注入")]
public class PropertyInjectionController(IPropertyInjectionTestService pi_test) : ControllerBase
{
    [Injection]
    private readonly ILogger<PropertyInjectionController>? _logger = null;

    [Injection]
    private readonly ITest1? _test1 = null;

    [Injection]
    private readonly IUserService<User>? _userService = null;

    [Injection]
    private readonly UserService<User>? _userService1 = null;

    /// <summary>
    /// 属性注入测试.
    /// </summary>
    [Injection]
    public ITest? Test { get; set; }

    /// <summary>
    /// </summary>
    [HttpGet]
    public async void Get()
    {
        _logger?.LogInformation("控制调用");
        Test?.Show();
        _test1?.Show();
        await pi_test.Execute();
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
using EasilyNET.Core.BaseType;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 当前用户
/// </summary>
[Route("api/[controller]/[action]"), ApiController]
public class CurrentUser : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// </summary>
    /// <param name="currentUser"></param>
    public CurrentUser(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// 得到当前用户
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public string GetUserId()
    {
        var userId = _currentUser.GetUserId<string>();
        return userId!;
    }
}
using EasilyNET.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 当前用户
/// </summary>
/// <remarks>
/// </remarks>
/// <param name="currentUser"></param>
[Route("api/[controller]"), ApiController]
public class CurrentUser(ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// 得到当前用户
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public string GetUserId()
    {
        var userId = currentUser.GetUserId<string>();
        return userId!;
    }
}
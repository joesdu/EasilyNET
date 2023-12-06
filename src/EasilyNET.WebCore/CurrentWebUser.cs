using EasilyNET.Core.Abstractions;
using EasilyNET.Core.Misc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EasilyNET.WebCore;

/// <summary>
/// 当前WEB下用户
/// </summary>
public class CurrentWebUser : ICurrentUser
{
    private readonly IHttpContextAccessor _context;

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    public CurrentWebUser(IHttpContextAccessor context)
    {
        context.NotNull(nameof(context));
        _context = context;
    }

    /// <inheritdoc />
    public TKey? GetUserId<TKey>(string type = ClaimTypes.NameIdentifier) => _context.HttpContext!.User.FindFirst(type)!.Value.ChangeType<TKey>();
}
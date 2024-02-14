using System.Security.Claims;

namespace EasilyNET.Core.Abstractions;

/// <summary>
/// 当前用户
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// 得到当前用户ID
    /// </summary>
    /// <param name="type">类型、sub或NameIdentifier</param>
    /// <typeparam name="TKey">动态键</typeparam>
    /// <returns></returns>
    public TKey? GetUserId<TKey>(string type = ClaimTypes.NameIdentifier);
}

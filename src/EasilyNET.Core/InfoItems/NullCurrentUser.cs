using EasilyNET.Core.Abstractions;
using System.Security.Claims;

namespace EasilyNET.Core;

/// <summary>
/// 可空
/// </summary>
public class NullCurrentUser : ICurrentUser
{
    /// <summary>
    /// 当前实例
    /// </summary>
    public static readonly NullCurrentUser Instance = new();

    /// <inheritdoc />
    public TKey? GetUserId<TKey>(string type = ClaimTypes.NameIdentifier) => default;
}
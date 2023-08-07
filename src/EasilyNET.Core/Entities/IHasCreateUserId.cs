namespace EasilyNET.Core.Entities;

/// <summary>
/// 创建用户ID
/// </summary>
/// <typeparam name="TUserKey"></typeparam>
public interface IHasCreateUserId<out TUserKey>
{
    /// <summary>
    /// 创建用户ID
    /// </summary>
    TUserKey? CreateUserId { get; }
}
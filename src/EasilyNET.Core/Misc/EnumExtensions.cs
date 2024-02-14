// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// 扩展枚举
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 转成显示名字
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string? ToDescription(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.ToDescription();
    }
}

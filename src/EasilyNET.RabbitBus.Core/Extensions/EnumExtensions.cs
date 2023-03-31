using System.ComponentModel;
using System.Reflection;

namespace EasilyNET.RabbitBus.Core.Extensions;

/// <summary>
/// 扩展枚举
/// </summary>
internal static class EnumExtensions
{
    /// <summary>
    /// 转成显示名字
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static string? ToDescription(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.ToDescription();
    }

    /// <summary>
    /// 获取描述或者显示名称
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    private static string ToDescription(this MemberInfo member)
    {
        var desc = member.GetCustomAttribute<DescriptionAttribute>();
        if (desc is not null) return desc.Description;
        //显示名
        var display = member.GetCustomAttribute<DisplayNameAttribute>();
        return display is not null ? display.DisplayName : member.Name;
    }
}
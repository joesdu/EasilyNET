// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System.ComponentModel;

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
    public static string ToDescription(this Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
        return fieldInfo is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : value.ToString();
    }
}
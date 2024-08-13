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

    /// <summary>
    /// 获取枚举所有值，排除指定的值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="exclude">要排除的枚举值</param>
    /// <returns>排除指定值后的枚举值集合</returns>
    public static IEnumerable<T> GetValues<T>(params T[] exclude) where T : Enum
    {
        var allValues = Enum.GetValues(typeof(T)).Cast<T>();
        return allValues.Except(exclude);
    }
}
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System.Collections.Concurrent;
using System.ComponentModel;

namespace EasilyNET.Core.Misc;

/// <summary>
/// 扩展枚举
/// </summary>
public static class EnumExtensions
{
    private static readonly ConcurrentDictionary<Enum, string> DescriptionCache = [];

    /// <summary>
    /// 转成显示名字
    /// </summary>
    /// <param name="value">枚举值</param>
    /// <returns>枚举值的描述</returns>
    public static string ToDescription(this Enum value)
    {
        return DescriptionCache.GetOrAdd(value, enumValue =>
        {
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
            var descriptionAttribute = fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            return descriptionAttribute?.Description ?? enumValue.ToString();
        });
    }

    /// <summary>
    /// 获取枚举所有值，排除指定的值
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="exclude">要排除的枚举值</param>
    /// <returns>排除指定值后的枚举值集合</returns>
    public static IEnumerable<T> GetValues<T>(params T[] exclude) where T : Enum
    {
        var allValues = Enum.GetValues(typeof(T)).Cast<T>();
        return exclude.Length == 0 ? allValues : allValues.Except(exclude);
    }
}
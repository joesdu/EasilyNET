// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System.Collections.Concurrent;
using System.ComponentModel;

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Enum extensions</para>
///     <para xml:lang="zh">扩展枚举</para>
/// </summary>
public static class EnumExtensions
{
    private static readonly ConcurrentDictionary<Enum, string> DescriptionCache = [];

    /// <summary>
    ///     <para xml:lang="en">Enum extensions</para>
    ///     <para xml:lang="zh">扩展枚举</para>
    /// </summary>
    extension(Enum value)
    {
        /// <summary>
        ///     <para xml:lang="en">Converts an enum value to its description</para>
        ///     <para xml:lang="zh">将枚举值转换为其描述</para>
        /// </summary>
        public string Description => DescriptionCache.GetOrAdd(value, enumValue =>
        {
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
            var descriptionAttribute = fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            return descriptionAttribute?.Description ?? enumValue.ToString();
        });
    }


    /// <summary>
    ///     <para xml:lang="en">Gets all enum values, excluding the specified values</para>
    ///     <para xml:lang="zh">获取枚举的所有值，排除指定的值</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The enum type</para>
    ///     <para xml:lang="zh">枚举类型</para>
    /// </typeparam>
    /// <param name="exclude">
    ///     <para xml:lang="en">The enum values to exclude</para>
    ///     <para xml:lang="zh">要排除的枚举值</para>
    /// </param>
    public static IEnumerable<T> GetValues<T>(params T[] exclude) where T : Enum
    {
        var allValues = Enum.GetValues(typeof(T)).Cast<T>();
        return exclude.Length == 0 ? allValues : allValues.Except(exclude);
    }
}
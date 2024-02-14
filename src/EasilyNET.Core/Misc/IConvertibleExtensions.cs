using System.ComponentModel;
using System.Globalization;

// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// 类型是否可直接转换扩展
/// </summary>
public static class IConvertibleExtensions
{
    /// <summary>
    /// 是否是数字类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsNumeric(this Type type) =>
        Type.GetTypeCode(type) switch
        {
            TypeCode.Byte    => true,
            TypeCode.SByte   => true,
            TypeCode.UInt16  => true,
            TypeCode.UInt32  => true,
            TypeCode.UInt64  => true,
            TypeCode.Int16   => true,
            TypeCode.Int32   => true,
            TypeCode.Int64   => true,
            TypeCode.Decimal => true,
            TypeCode.Double  => true,
            TypeCode.Single  => true,
            _                => false
        };

    /// <summary>
    /// 类型直转
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="InvalidCastException"></exception>
    public static T? ConvertTo<T>(this IConvertible? value) where T : IConvertible
    {
        if (value == null || Equals(value, DBNull.Value))
        {
            return default;
        }
        var type = typeof(T);
        if (value.GetType() == type) return (T)value;
        if (type.IsNumeric()) return (T)value.ToType(type, new NumberFormatInfo());
        if (type.IsEnum) return (T)Enum.Parse(type, value.ToString(CultureInfo.InvariantCulture));
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return value is Enum enumValue ? (T)Enum.ToObject(Nullable.GetUnderlyingType(type)!, enumValue) : (T)value;
        var converter = TypeDescriptor.GetConverter(value);
        if (converter.CanConvertTo(type)) return (T?)converter.ConvertTo(value, type);
        converter = TypeDescriptor.GetConverter(type);
        return converter.CanConvertFrom(value.GetType()) ? (T?)converter.ConvertFrom(value) : throw new InvalidCastException($"Cannot convert {value} to {type}");
    }

    /// <summary>
    /// 类型直转
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="defaultValue">转换失败的默认值</param>
    /// <returns></returns>
    public static T TryConvertTo<T>(this IConvertible value, T defaultValue = default!) where T : IConvertible => value.ConvertTo<T>() ?? defaultValue;
}

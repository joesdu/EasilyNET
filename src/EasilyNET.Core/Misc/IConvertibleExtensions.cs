using System.ComponentModel;
using System.Globalization;

// ReSharper disable UnusedMember.Global
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
    /// <param name="type">要检查的类型</param>
    /// <returns>如果是数字类型，则为 <see langword="true" />，否则为 <see langword="false" /></returns>
    public static bool IsNumeric(this Type type) =>
        Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => true,
            TypeCode.SByte => true,
            TypeCode.UInt16 => true,
            TypeCode.UInt32 => true,
            TypeCode.UInt64 => true,
            TypeCode.Int16 => true,
            TypeCode.Int32 => true,
            TypeCode.Int64 => true,
            TypeCode.Decimal => true,
            TypeCode.Double => true,
            TypeCode.Single => true,
            _ => false
        };

    /// <summary>
    /// 类型直转
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="value">要转换的值</param>
    /// <returns>转换后的值</returns>
    /// <exception cref="InvalidCastException">如果无法转换类型，则抛出异常</exception>
    public static T? ConvertTo<T>(this IConvertible? value) where T : IConvertible
    {
        if (value == null || Equals(value, DBNull.Value)) return default;
        var targetType = typeof(T);
        var sourceType = value.GetType();
        // 如果源类型和目标类型相同，直接返回
        if (sourceType == targetType) return (T)value;
        // 优化数字类型转换
        if (targetType.IsNumeric())
        {
            return targetType switch
            {
                _ when targetType == typeof(byte) => (T)(object)Convert.ToByte(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(sbyte) => (T)(object)Convert.ToSByte(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(ushort) => (T)(object)Convert.ToUInt16(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(uint) => (T)(object)Convert.ToUInt32(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(ulong) => (T)(object)Convert.ToUInt64(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(short) => (T)(object)Convert.ToInt16(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(int) => (T)(object)Convert.ToInt32(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(long) => (T)(object)Convert.ToInt64(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(decimal) => (T)(object)Convert.ToDecimal(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(double) => (T)(object)Convert.ToDouble(value, CultureInfo.InvariantCulture),
                _ when targetType == typeof(float) => (T)(object)Convert.ToSingle(value, CultureInfo.InvariantCulture),
                _ => throw new InvalidCastException($"Cannot convert {value} to {targetType}")
            };
        }
        // 优化枚举类型转换
        if (targetType.IsEnum) return (T)Enum.Parse(targetType, value.ToString(CultureInfo.InvariantCulture));
        // 优化可空类型转换
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType)!;
            return value is Enum enumValue ? (T)Enum.ToObject(underlyingType, enumValue) : (T)value.ToType(underlyingType, CultureInfo.InvariantCulture);
        }
        // 添加对 Guid 类型的支持
        if (targetType == typeof(Guid))
        {
            return (T)(object)Guid.Parse(value.ToString(CultureInfo.InvariantCulture));
        }
        // 使用 TypeDescriptor 进行类型转换
        var converter = TypeDescriptor.GetConverter(value);
        if (converter.CanConvertTo(targetType)) return (T?)converter.ConvertTo(value, targetType);
        converter = TypeDescriptor.GetConverter(targetType);
        return converter.CanConvertFrom(sourceType)
                   ? (T?)converter.ConvertFrom(value)
                   : throw new InvalidCastException($"Cannot convert {value} to {targetType}");
    }

    /// <summary>
    /// 类型直转
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="value">要转换的值</param>
    /// <param name="convertible">转换后的值</param>
    /// <returns>是否转换成功</returns>
    public static bool TryConvertTo<T>(this IConvertible? value, out T? convertible) where T : IConvertible
    {
        convertible = default;
        try
        {
            convertible = (T?)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
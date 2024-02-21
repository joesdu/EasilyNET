// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System.Globalization;

#pragma warning disable IDE0046

namespace EasilyNET.Core.Misc;

/// <summary>
/// 时间戳相关扩展
/// </summary>
public static class DateTimeStampExtension
{
    /// <summary>
    /// 获取 Unix 纪元日期时间(1970-01-01)(UTC时间)
    /// </summary>
    public static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 获取自 DateTime.MaxValue 的 Unix 纪元以来的毫秒数(UTC+0).
    /// </summary>
    public static long DateTimeMaxValueMillisecondsSinceEpoch => (DateTime.MaxValue - UnixEpoch).Ticks / TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// 获取自 DateTime.MinValue 的 Unix 纪元以来的毫秒数(UTC+0).
    /// </summary>
    public static long DateTimeMinValueMillisecondsSinceEpoch => (DateTime.MinValue - UnixEpoch).Ticks / TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// 获取自 DateTime.MaxValue 的 Unix 纪元以来的秒数(UTC+0).
    /// </summary>
    public static long DateTimeMaxValueSecondsSinceEpoch => (DateTime.MaxValue - UnixEpoch).Ticks / TimeSpan.TicksPerSecond;

    /// <summary>
    /// 获取自 DateTime.MinValue 的 Unix 纪元以来的秒数(UTC+0).
    /// </summary>
    public static long DateTimeMinValueSecondsSinceEpoch => (DateTime.MinValue - UnixEpoch).Ticks / TimeSpan.TicksPerSecond;

    /// <summary>
    /// 从自 Unix 纪元以来的毫秒数转换为日期时间(UTC+0).
    /// </summary>
    /// <param name="millisecondsSinceEpoch">自 Unix 纪元以来的毫秒数.</param>
    /// <returns>A DateTime.</returns>
    public static DateTime ToDateTimeFromMillisecondsSinceEpoch(this long millisecondsSinceEpoch)
    {
        if (millisecondsSinceEpoch >= DateTimeMinValueMillisecondsSinceEpoch && millisecondsSinceEpoch <= DateTimeMaxValueMillisecondsSinceEpoch)
            return millisecondsSinceEpoch == DateTimeMaxValueMillisecondsSinceEpoch
                       ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
                       : UnixEpoch.AddTicks(millisecondsSinceEpoch * TimeSpan.TicksPerMillisecond);
        var message = $"The value {millisecondsSinceEpoch} for the BsonDateTime MillisecondsSinceEpoch is outside the range that can be converted to a .NET DateTime.";
        throw new ArgumentOutOfRangeException(nameof(millisecondsSinceEpoch), message);
    }

    /// <summary>
    /// 从自 Unix 纪元以来的秒数转换为日期时间(UTC+0).
    /// </summary>
    /// <param name="secondsSinceEpoch">自 Unix 纪元以来的秒数.</param>
    /// <returns>A DateTime.</returns>
    public static DateTime ToDateTimeFromSecondsSinceEpoch(this long secondsSinceEpoch)
    {
        if (secondsSinceEpoch >= DateTimeMinValueSecondsSinceEpoch && secondsSinceEpoch <= DateTimeMaxValueSecondsSinceEpoch)
            return secondsSinceEpoch == DateTimeMaxValueSecondsSinceEpoch
                       ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
                       : UnixEpoch.AddTicks(secondsSinceEpoch * TimeSpan.TicksPerSecond);
        var message = $"The value {secondsSinceEpoch} for the BsonDateTime SecondsSinceEpoch is outside the range that can be converted to a .NET DateTime.";
        throw new ArgumentOutOfRangeException(nameof(secondsSinceEpoch), message);
    }

    /// <summary>
    /// 将日期时间转换为自 Unix 纪元以来的毫秒数.
    /// </summary>
    /// <param name="dateTime">A DateTime.</param>
    /// <returns>自 Unix 纪元以来的毫秒数(UTC+0).</returns>
    public static long ToMillisecondsSinceEpoch(this DateTime dateTime)
    {
        var utcDateTime = dateTime.ToUniversalTime();
        return (utcDateTime - UnixEpoch).Ticks / 10000;
    }

    /// <summary>
    /// 将日期时间转换为自 Unix 纪元以来的秒数.
    /// </summary>
    /// <param name="dateTime">A DateTime.</param>
    /// <returns>Number of seconds since Unix epoch.</returns>
    public static long ToSecondsSinceEpoch(this DateTime dateTime)
    {
        var utcDateTime = dateTime.ToUniversalTime();
        return (utcDateTime - UnixEpoch).Ticks / TimeSpan.TicksPerSecond;
    }

    /// <summary>
    /// 将TimeSpan转化成字符串
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToString(TimeSpan value)
    {
        const int msInOneSecond = 1000;
        const int msInOneMinute = 60 * msInOneSecond;
        const int msInOneHour = 60 * msInOneMinute;
        var ms = (long)value.TotalMilliseconds;
        if (ms % msInOneHour == 0)
        {
            return $"{ms / msInOneHour}h";
        }
        if (ms % msInOneMinute == 0 && ms < msInOneHour)
        {
            return $"{ms / msInOneMinute}m";
        }
        if (ms % msInOneSecond == 0 && ms < msInOneMinute)
        {
            return $"{ms / msInOneSecond}s";
        }
        return ms < 1000 ? $"{ms}ms" : value.ToString();
    }

    /// <summary>
    /// 将字符串转化成TimeSpan
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static TimeSpan Parse(string value) => !TryParse(value, out var result) ? throw new FormatException($"Invalid TimeSpan value: \"{value}\".") : result;

    /// <summary>
    /// 尝试将字符串转化成TimeSpan
    /// </summary>
    /// <param name="value"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryParse(string value, out TimeSpan result)
    {
        if (!string.IsNullOrEmpty(value))
        {
            value = value.ToLowerInvariant();
            var end = value.Length - 1;
            var multiplier = 1000; // default units are seconds
            switch (value[end])
            {
                case 's' when value[end - 1] == 'm':
                    value = value[..^2];
                    multiplier = 1;
                    break;
                case 's':
                    value = value[..^1];
                    multiplier = 1000;
                    break;
                case 'm':
                    value = value[..^1];
                    multiplier = 60 * 1000;
                    break;
                case 'h':
                    value = value[..^1];
                    multiplier = 60 * 60 * 1000;
                    break;
                default:
                {
                    if (value.Contains(':'))
                    {
                        return TimeSpan.TryParse(value, out result);
                    }
                    break;
                }
            }
            const NumberStyles numberStyles = NumberStyles.None;
            if (double.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out var multiplicand))
            {
                result = TimeSpan.FromMilliseconds(multiplicand * multiplier);
                return true;
            }
        }
        result = default;
        return false;
    }
}
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System.Globalization;

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Extensions related to timestamps</para>
///     <para xml:lang="zh">时间戳相关扩展</para>
/// </summary>
public static class DateTimeStampExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Gets the Unix epoch date and time (1970-01-01) (UTC time)</para>
    ///     <para xml:lang="zh">获取 Unix 纪元日期时间(1970-01-01)(UTC时间)</para>
    /// </summary>
    public static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    ///     <para xml:lang="en">Gets the number of milliseconds since the Unix epoch from DateTime.MaxValue (UTC+0)</para>
    ///     <para xml:lang="zh">获取自 DateTime.MaxValue 的 Unix 纪元以来的毫秒数(UTC+0)</para>
    /// </summary>
    public static long DateTimeMaxValueMillisecondsSinceEpoch => (DateTime.MaxValue - UnixEpoch).Ticks / TimeSpan.TicksPerMillisecond;

    /// <summary>
    ///     <para xml:lang="en">Gets the number of milliseconds since the Unix epoch from DateTime.MinValue (UTC+0)</para>
    ///     <para xml:lang="zh">获取自 DateTime.MinValue 的 Unix 纪元以来的毫秒数(UTC+0)</para>
    /// </summary>
    public static long DateTimeMinValueMillisecondsSinceEpoch => (DateTime.MinValue - UnixEpoch).Ticks / TimeSpan.TicksPerMillisecond;

    /// <summary>
    ///     <para xml:lang="en">Gets the number of seconds since the Unix epoch from DateTime.MaxValue (UTC+0)</para>
    ///     <para xml:lang="zh">获取自 DateTime.MaxValue 的 Unix 纪元以来的秒数(UTC+0)</para>
    /// </summary>
    public static long DateTimeMaxValueSecondsSinceEpoch => (DateTime.MaxValue - UnixEpoch).Ticks / TimeSpan.TicksPerSecond;

    /// <summary>
    ///     <para xml:lang="en">Gets the number of seconds since the Unix epoch from DateTime.MinValue (UTC+0)</para>
    ///     <para xml:lang="zh">获取自 DateTime.MinValue 的 Unix 纪元以来的秒数(UTC+0)</para>
    /// </summary>
    public static long DateTimeMinValueSecondsSinceEpoch => (DateTime.MinValue - UnixEpoch).Ticks / TimeSpan.TicksPerSecond;

    /// <summary>
    ///     <para xml:lang="en">Converts milliseconds since the Unix epoch to a DateTime (UTC+0)</para>
    ///     <para xml:lang="zh">从自 Unix 纪元以来的毫秒数转换为日期时间(UTC+0)</para>
    /// </summary>
    /// <param name="millisecondsSinceEpoch">
    ///     <para xml:lang="en">The number of milliseconds since the Unix epoch</para>
    ///     <para xml:lang="zh">自 Unix 纪元以来的毫秒数</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A DateTime</para>
    ///     <para xml:lang="zh">一个 DateTime</para>
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when the value is outside the range that can be converted to a .NET DateTime</para>
    ///     <para xml:lang="zh">当值超出可以转换为 .NET DateTime 的范围时抛出</para>
    /// </exception>
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
    ///     <para xml:lang="en">Converts seconds since the Unix epoch to a DateTime (UTC+0)</para>
    ///     <para xml:lang="zh">从自 Unix 纪元以来的秒数转换为日期时间(UTC+0)</para>
    /// </summary>
    /// <param name="secondsSinceEpoch">
    ///     <para xml:lang="en">The number of seconds since the Unix epoch</para>
    ///     <para xml:lang="zh">自 Unix 纪元以来的秒数</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A DateTime</para>
    ///     <para xml:lang="zh">一个 DateTime</para>
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when the value is outside the range that can be converted to a .NET DateTime</para>
    ///     <para xml:lang="zh">当值超出可以转换为 .NET DateTime 的范围时抛出</para>
    /// </exception>
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
    ///     <para xml:lang="en">Converts a DateTime to the number of milliseconds since the Unix epoch</para>
    ///     <para xml:lang="zh">将日期时间转换为自 Unix 纪元以来的毫秒数</para>
    /// </summary>
    /// <param name="dateTime">
    ///     <para xml:lang="en">A DateTime</para>
    ///     <para xml:lang="zh">一个 DateTime</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The number of milliseconds since the Unix epoch (UTC+0)</para>
    ///     <para xml:lang="zh">自 Unix 纪元以来的毫秒数(UTC+0)</para>
    /// </returns>
    public static long ToMillisecondsSinceEpoch(this DateTime dateTime)
    {
        var utcDateTime = dateTime.ToUniversalTime();
        return (utcDateTime - UnixEpoch).Ticks / 10000;
    }

    /// <summary>
    ///     <para xml:lang="en">Converts a DateTime to the number of seconds since the Unix epoch</para>
    ///     <para xml:lang="zh">将日期时间转换为自 Unix 纪元以来的秒数</para>
    /// </summary>
    /// <param name="dateTime">
    ///     <para xml:lang="en">A DateTime</para>
    ///     <para xml:lang="zh">一个 DateTime</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The number of seconds since the Unix epoch</para>
    ///     <para xml:lang="zh">自 Unix 纪元以来的秒数</para>
    /// </returns>
    public static long ToSecondsSinceEpoch(this DateTime dateTime)
    {
        var utcDateTime = dateTime.ToUniversalTime();
        return (utcDateTime - UnixEpoch).Ticks / TimeSpan.TicksPerSecond;
    }

    /// <summary>
    ///     <para xml:lang="en">Converts a TimeSpan to a string</para>
    ///     <para xml:lang="zh">将 TimeSpan 转换为字符串</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The TimeSpan to convert</para>
    ///     <para xml:lang="zh">要转换的 TimeSpan</para>
    /// </param>
    /// <returns />
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
    ///     <para xml:lang="en">Converts a string to a TimeSpan</para>
    ///     <para xml:lang="zh">将字符串转换为 TimeSpan</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The string to convert</para>
    ///     <para xml:lang="zh">要转换的字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The corresponding TimeSpan</para>
    ///     <para xml:lang="zh">对应的 TimeSpan</para>
    /// </returns>
    /// <exception cref="FormatException">
    ///     <para xml:lang="en">Thrown when the string is not a valid TimeSpan</para>
    ///     <para xml:lang="zh">当字符串不是有效的 TimeSpan 时抛出</para>
    /// </exception>
    public static TimeSpan Parse(string value) => !TryParse(value, out var result) ? throw new FormatException($"Invalid TimeSpan value: \"{value}\".") : result;

    /// <summary>
    ///     <para xml:lang="en">Tries to convert a string to a TimeSpan</para>
    ///     <para xml:lang="zh">尝试将字符串转换为 TimeSpan</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The string to convert</para>
    ///     <para xml:lang="zh">要转换的字符串</para>
    /// </param>
    /// <param name="result">
    ///     <para xml:lang="en">The resulting TimeSpan</para>
    ///     <para xml:lang="zh">转换结果 TimeSpan</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the conversion was successful, otherwise false</para>
    ///     <para xml:lang="zh">如果转换成功则为 true，否则为 false</para>
    /// </returns>
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
                    multiplier = 60_000;
                    break;
                case 'h':
                    value = value[..^1];
                    multiplier = 3_600_000;
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
        result = TimeSpan.Zero;
        return false;
    }
}
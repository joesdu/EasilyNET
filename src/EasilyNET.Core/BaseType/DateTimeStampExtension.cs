// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.BaseType;

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
}
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// <see cref="DateOnly" /> | <see cref="TimeOnly" />扩展
/// </summary>
public static class DateTimeOnlyExtensions
{
    /// <summary>
    /// 从Ticks转换为TimeOnly
    /// </summary>
    /// <param name="ticks"></param>
    /// <returns></returns>
    public static TimeOnly ToTimeOnly(this long ticks) => TimeOnly.FromTimeSpan(TimeSpan.FromTicks(ticks));

    /// <summary>
    /// 将DateTime转化成TimeOnly
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static TimeOnly ToTimeOnly(this DateTime dateTime) => TimeOnly.FromDateTime(dateTime);

    /// <summary>
    /// 从Ticks转换为DateOnly
    /// </summary>
    /// <param name="ticks"></param>
    /// <returns></returns>
    public static DateOnly ToDateOnly(this long ticks) => DateOnly.FromDateTime(new(ticks, DateTimeKind.Local));

    /// <summary>
    /// 将DateTime转化成DateOnly
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateOnly ToDateOnly(this DateTime dateTime) => DateOnly.FromDateTime(dateTime);
}
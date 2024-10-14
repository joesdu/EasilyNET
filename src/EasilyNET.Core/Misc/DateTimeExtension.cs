// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

using System.Globalization;
using EasilyNET.Core.Enums;

namespace EasilyNET.Core.Misc;

/// <summary>
/// DateTime扩展
/// </summary>
public static class DateTimeExtension
{
    /// <summary>
    /// 获取某天开始时间
    /// </summary>
    /// <param name="dateTime">某天中的任意时间</param>
    /// <returns></returns>
    public static DateTime DayStart(this DateTime dateTime) => dateTime.Date;

    /// <summary>
    /// 获取某天结束时间
    /// </summary>
    /// <param name="dateTime">某天中的任意时间</param>
    /// <returns></returns>
    public static DateTime DayEnd(this DateTime dateTime) => dateTime.DayStart().AddDays(1).AddMilliseconds(-1);

    /// <summary>
    /// 获取某天的始末时间
    /// </summary>
    /// <param name="dateTime">某天中的任意时间</param>
    /// <returns>(Start, End)</returns>
    public static ValueTuple<DateTime, DateTime> DayStartEnd(this DateTime dateTime) => new(dateTime.DayStart(), dateTime.DayEnd());

    /// <summary>
    /// 获取某天的所属周的开始时间
    /// </summary>
    /// <param name="dateTime">某周中的任意天日期</param>
    /// <param name="firstDay">一周的第一天[周日还是周一或者其他]</param>
    /// <returns></returns>
    public static DateTime WeekStart(this DateTime dateTime, DayOfWeek firstDay) => dateTime.AddDays(-dateTime.DayOfWeek.DayNumber()).DayStart().AddDays((int)firstDay);

    /// <summary>
    /// 获取某天的所属周的结束时间
    /// </summary>
    /// <param name="dateTime">某周中的任意天日期</param>
    /// <param name="firstDay">一周的第一天[周日还是周一或者其他]</param>
    /// <returns></returns>
    public static DateTime WeekEnd(this DateTime dateTime, DayOfWeek firstDay) => dateTime.WeekStart(firstDay).AddDays(6).DayEnd();

    /// <summary>
    /// 获取某天所属周的开始和结束时间
    /// </summary>
    /// <param name="dateTime">某周中的任意天日期</param>
    /// <param name="firstDay">一周的第一天[周日还是周一或者其他]</param>
    /// <returns></returns>
    public static ValueTuple<DateTime, DateTime> WeekStartEnd(this DateTime dateTime, DayOfWeek firstDay) => new(dateTime.WeekStart(firstDay), dateTime.WeekEnd(firstDay));

    /// <summary>
    /// 获取某月的开始时间
    /// </summary>
    /// <param name="dateTime">某月中的任意天日期</param>
    /// <returns></returns>
    public static DateTime MonthStart(this DateTime dateTime) => dateTime.DayStart().AddDays(1 - dateTime.Day);

    /// <summary>
    /// 获取某月的结束时间
    /// </summary>
    /// <param name="dateTime">某月中的任意天日期</param>
    /// <returns></returns>
    public static DateTime MonthEnd(this DateTime dateTime) => dateTime.MonthStart().AddMonths(1).AddMilliseconds(-1);

    /// <summary>
    /// 获取某月的始末时间
    /// </summary>
    /// <param name="dateTime">某天中的任意时间</param>
    /// <returns>(Start, End)</returns>
    public static ValueTuple<DateTime, DateTime> MonthStartEnd(this DateTime dateTime) => new(dateTime.MonthStart(), dateTime.MonthEnd());

    /// <summary>
    /// 获取某年的开始时间
    /// </summary>
    /// <param name="dateTime">某年中的任意一天</param>
    /// <returns></returns>
    public static DateTime YearStart(this DateTime dateTime) => dateTime.Date.AddMonths(1 - dateTime.Month).AddDays(1 - dateTime.Day).DayStart();

    /// <summary>
    /// 获取某年的结束时间
    /// </summary>
    /// <param name="dateTime">某年中的任意一天</param>
    /// <returns></returns>
    public static DateTime YearEnd(this DateTime dateTime) => dateTime.YearStart().AddYears(1).AddMilliseconds(-1);

    /// <summary>
    /// 获取某年的始末时间
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns>(Start, End)</returns>
    public static ValueTuple<DateTime, DateTime> YearStartEnd(this DateTime dateTime) => new(dateTime.YearStart(), dateTime.YearEnd());

    /// <summary>
    /// 根据周数和年份获取某周的开始和结束时间
    /// </summary>
    /// <param name="week">一年中自然周数</param>
    /// <param name="year">年份</param>
    /// <param name="firstDay">一周开始时间(周一或者周日)</param>
    /// <returns></returns>
    public static ValueTuple<DateTime, DateTime> WeekStartEndByNumber(this int week, int year, DayOfWeek firstDay) => new DateTime(year, 1, 1).AddDays((week - 1) * 7).WeekStartEnd(firstDay);

    /// <summary>
    /// 根据月份获取某月的开始时间和结束时间
    /// </summary>
    /// <param name="month">月份</param>
    /// <param name="year">年份</param>
    /// <returns></returns>
    public static ValueTuple<DateTime, DateTime> MonthStartEndByMonth(this int month, int year) => (month < 1) | (month > 13) ? throw new("非法月份") : new DateTime(year, month, 2).MonthStartEnd();

    /// <summary>
    /// 年份👉DateTime(某年的初始时间)
    /// </summary>
    /// <param name="year">年份</param>
    /// <returns></returns>
    public static DateTime YearToDateTime(this int year) => new(year, 1, 1);

    /// <summary>
    /// 获取整周的星期数字形式
    /// </summary>
    /// <param name="day"></param>
    /// <returns></returns>
    public static int DayNumber(this DayOfWeek day) =>
        day switch
        {
            DayOfWeek.Friday => 5,
            DayOfWeek.Monday => 1,
            DayOfWeek.Saturday => 6,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            _ => 7
        };

    /// <summary>
    /// 将0-7的数字转化成DayOfWeek类型
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static DayOfWeek ToDayOfWeek(this int number) =>
        (number > 7) | (number < 0)
            ? throw new("please input 0-7")
            : number switch
            {
                0 => DayOfWeek.Sunday,
                1 => DayOfWeek.Monday,
                2 => DayOfWeek.Tuesday,
                3 => DayOfWeek.Wednesday,
                4 => DayOfWeek.Thursday,
                5 => DayOfWeek.Friday,
                6 => DayOfWeek.Saturday,
                _ => DayOfWeek.Sunday
            };

    /// <summary>
    /// 获取一周星期对应中文名
    /// </summary>
    /// <param name="day"></param>
    /// <param name="type"> 1 ? "周" : "星期"</param>
    /// <returns>周(一至日(天))||星期(一至日(天))</returns>
    public static string DayName(this int day, int type = 1)
    {
        var name = day switch
        {
            1 => "一",
            2 => "二",
            3 => "三",
            4 => "四",
            5 => "五",
            6 => "六",
            0 => type == 1 ? "日" : "天",
            _ => "错误"
        };
        return $"{(type == 1 ? "周" : "星期")}{name}";
    }

    /// <summary>
    /// 获取一周星期对应中文名
    /// </summary>
    /// <param name="day"></param>
    /// <param name="type"> 1 ? "周" : "星期"</param>
    /// <returns>周(一至日(天))||星期(一至日(天))</returns>
    public static string DayName(this DayOfWeek day, int type = 1)
    {
        var name = day switch
        {
            DayOfWeek.Monday => "一",
            DayOfWeek.Tuesday => "二",
            DayOfWeek.Wednesday => "三",
            DayOfWeek.Thursday => "四",
            DayOfWeek.Friday => "五",
            DayOfWeek.Saturday => "六",
            DayOfWeek.Sunday => type == 1 ? "日" : "天",
            _ => "错误"
        };
        return $"{(type == 1 ? "周" : "星期")}{name}";
    }

    /// <summary>
    /// 验证时间段和另一个时间段的重合情况
    /// </summary>
    /// <param name="sub">需要验证的时间段</param>
    /// <param name="source">所属源</param>
    /// <returns>ETimeOverlap</returns>
    public static ETimeOverlap TimeOverlap(Tuple<DateTime, DateTime> sub, Tuple<DateTime, DateTime> source)
    {
        var (subStart, subEnd) = sub;
        var (validateStart, validateEnd) = source;
        return (subStart < validateEnd && validateStart < subEnd) switch
        {
            true when subStart >= validateStart && subEnd <= validateEnd => ETimeOverlap.完全重合,
            true when subStart < validateStart && subEnd >= validateStart && subEnd < validateEnd => ETimeOverlap.后段重合,
            true when subStart > validateStart && subStart < validateEnd && subEnd > validateEnd => ETimeOverlap.前段重合,
            _ => ETimeOverlap.完全不重合
        };
    }

    /// <summary>
    /// 获取某个日期从另一个日期开始的间隔周数,当要计算的日期小于起始日期时,返回-1
    /// </summary>
    /// <param name="point">起始日期</param>
    /// <param name="date">要计算的日期</param>
    /// <returns></returns>
    public static int WeekNoFromPoint(DateTime point, DateTime? date)
    {
        date ??= DateTime.Now;
        if (date < point) return -1;
        var dayDiff = point.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)point.DayOfWeek - 1;
        var first_monday = point.AddDays(-dayDiff);
        var daysCount = (date.Value - first_monday).TotalDays;
        return (int)(daysCount / 7) + (daysCount % 7 == 0 ? 0 : 1);
    }

    /// <summary>
    /// 将DateTime转化成DateOnly
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateOnly ToDateOnly(this DateTime dateTime) => DateOnly.FromDateTime(dateTime);

    /// <summary>
    /// 将DateTime转化成TimeOnly
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static TimeOnly ToTimeOnly(this DateTime dateTime) => TimeOnly.FromDateTime(dateTime);

    /// <summary>
    /// 将 DateTime 转换为 byte[]
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static byte[] ToByteArray(this DateTime dateTime)
    {
        var ticks = dateTime.Ticks;
        var bytes = BitConverter.GetBytes(ticks);
        return bytes;
    }

    /// <summary>
    /// 获取某日期所在周是当年的第几周
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="cultureInfo">区域信息,默认:当前区域</param>
    /// <returns></returns>
    public static int GetWeekOfYear(this DateTime date, CultureInfo? cultureInfo = null)
    {
        var culture = cultureInfo ?? CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    /// <summary>
    /// 获取某日期所在周是当年的第几周(当前所在区域)
    /// </summary>
    /// <param name="date"></param>
    /// <param name="cultureInfo">区域信息,默认:当前区域</param>
    /// <returns></returns>
    public static int GetWeekOfYear(this DateOnly date, CultureInfo? cultureInfo = null) => GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local), cultureInfo);
}
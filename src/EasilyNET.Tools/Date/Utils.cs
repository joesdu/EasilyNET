using EasilyNET.Core.Enums;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Tools;

/// <summary>
/// 工具类
/// </summary>
public static class Utils
{
    /// <summary>
    /// 获取整周的星期数字形式
    /// </summary>
    /// <param name="day"></param>
    /// <returns></returns>
    public static int DayNumber(this DayOfWeek day) =>
        day switch
        {
            DayOfWeek.Friday    => 5,
            DayOfWeek.Monday    => 1,
            DayOfWeek.Saturday  => 6,
            DayOfWeek.Thursday  => 4,
            DayOfWeek.Tuesday   => 2,
            DayOfWeek.Wednesday => 3,
            _                   => 7
        };

    /// <summary>
    /// 将0-7的数字转化成DayOfWeek类型
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static DayOfWeek ToDayOfWeek(this int number) =>
        number > 7 | number < 0
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
            DayOfWeek.Monday    => "一",
            DayOfWeek.Tuesday   => "二",
            DayOfWeek.Wednesday => "三",
            DayOfWeek.Thursday  => "四",
            DayOfWeek.Friday    => "五",
            DayOfWeek.Saturday  => "六",
            DayOfWeek.Sunday    => type == 1 ? "日" : "天",
            _                   => "错误"
        };
        return $"{(type == 1 ? "周" : "星期")}{name}";
    }

    /// <summary>
    /// 验证时间段和另一个时间段的重合情况
    /// </summary>
    /// <param name="sub">需要验证的时间段</param>
    /// <param name="validate">所属源</param>
    /// <returns>ETimeOverlap</returns>
    public static ETimeOverlap TimeOverlap(Tuple<DateTime, DateTime> sub, Tuple<DateTime, DateTime> validate)
    {
        var (subStart, subEnd) = sub;
        var (validateStart, validateEnd) = validate;
        return (subStart < validateEnd && validateStart < subEnd) switch
        {
            true when subStart >= validateStart && subEnd <= validateEnd                          => ETimeOverlap.完全重合,
            true when subStart < validateStart && subEnd >= validateStart && subEnd < validateEnd => ETimeOverlap.后段重合,
            true when subStart > validateStart && subStart < validateEnd && subEnd > validateEnd  => ETimeOverlap.前段重合,
            _                                                                                     => ETimeOverlap.完全不重合
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
}
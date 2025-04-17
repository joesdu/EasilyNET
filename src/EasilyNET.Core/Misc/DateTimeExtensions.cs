// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

using System.Globalization;
using EasilyNET.Core.Enums;

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">DateTime extensions</para>
///     <para xml:lang="zh">DateTime 扩展</para>
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    ///     <para xml:lang="en">DateTime extensions</para>
    ///     <para xml:lang="zh">DateTime 扩展</para>
    /// </summary>
    extension(DateTime dt)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the date only from specific datetime</para>
        ///     <para xml:lang="zh">获取仅包含日期部分</para>
        /// </summary>
        public DateOnly DateOnly => DateOnly.FromDateTime(dt);

        /// <summary>
        ///     <para xml:lang="en">Gets the time only from specific datetime</para>
        ///     <para xml:lang="zh">获取仅包含时间部分</para>
        /// </summary>
        public TimeOnly TimeOnly => TimeOnly.FromDateTime(dt);

        /// <summary>
        ///     <para xml:lang="en">Gets the start time of a specific day</para>
        ///     <para xml:lang="zh">获取某天的开始时间</para>
        /// </summary>
        public DateTime DayStart => dt.Date;

        /// <summary>
        ///     <para xml:lang="en">Gets the end time of a specific day</para>
        ///     <para xml:lang="zh">获取某天的结束时间</para>
        /// </summary>
        public DateTime DayEnd => dt.Date.AddDays(1).AddMilliseconds(-1);

        /// <summary>
        ///     <para xml:lang="en">Gets the start and end time of a specific day</para>
        ///     <para xml:lang="zh">获取某天的开始和结束时间</para>
        /// </summary>
        public ValueTuple<DateTime, DateTime> DayStartEnd => new(dt.DayStart, dt.DayEnd);

        /// <summary>
        ///     <para xml:lang="en">Gets the start time of a specific month</para>
        ///     <para xml:lang="zh">获取某月的开始时间</para>
        /// </summary>
        public DateTime MonthStart => dt.DayStart.AddDays(1 - dt.Day);

        /// <summary>
        ///     <para xml:lang="en">Gets the end time of a specific month</para>
        ///     <para xml:lang="zh">获取某月的结束时间</para>
        /// </summary>
        public DateTime MonthEnd => dt.MonthStart.AddMonths(1).AddMilliseconds(-1);

        /// <summary>
        ///     <para xml:lang="en">Gets the start and end time of a specific month</para>
        ///     <para xml:lang="zh">获取某月的开始和结束时间</para>
        /// </summary>
        public ValueTuple<DateTime, DateTime> MonthStartEnd => new(dt.MonthStart, dt.MonthEnd);

        /// <summary>
        ///     <para xml:lang="en">Gets the start time of a specific year</para>
        ///     <para xml:lang="zh">获取某年的开始时间</para>
        /// </summary>
        public DateTime YearStart => dt.Date.AddMonths(1 - dt.Month).AddDays(1 - dt.Day).DayStart;

        /// <summary>
        ///     <para xml:lang="en">Gets the end time of a specific year</para>
        ///     <para xml:lang="zh">获取某年的结束时间</para>
        /// </summary>
        public DateTime YearEnd => dt.YearStart.AddYears(1).AddMilliseconds(-1);

        /// <summary>
        ///     <para xml:lang="en">Gets the start and end time of a specific year</para>
        ///     <para xml:lang="zh">获取某年的开始和结束时间</para>
        /// </summary>
        public ValueTuple<DateTime, DateTime> YearStartEnd => new(dt.YearStart, dt.YearEnd);

        /// <summary>
        ///     <para xml:lang="en">Gets the start time of the week that a specific day belongs to</para>
        ///     <para xml:lang="zh">获取某天所属周的开始时间</para>
        /// </summary>
        /// <param name="firstDay">
        ///     <para xml:lang="en">The first day of the week (Sunday, Monday, etc.)</para>
        ///     <para xml:lang="zh">一周的第一天（周日、周一等）</para>
        /// </param>
        public DateTime WeekStart(DayOfWeek firstDay) => dt.AddDays(-dt.DayOfWeek.DayNumber()).DayStart.AddDays((int)firstDay);

        /// <summary>
        ///     <para xml:lang="en">Gets the end time of the week that a specific day belongs to</para>
        ///     <para xml:lang="zh">获取某天所属周的结束时间</para>
        /// </summary>
        /// <param name="firstDay">
        ///     <para xml:lang="en">The first day of the week (Sunday, Monday, etc.)</para>
        ///     <para xml:lang="zh">一周的第一天（周日、周一等）</para>
        /// </param>
        public DateTime WeekEnd(DayOfWeek firstDay) => dt.WeekStart(firstDay).AddDays(6).DayEnd;

        /// <summary>
        ///     <para xml:lang="en">Gets the start and end time of the week that a specific day belongs to</para>
        ///     <para xml:lang="zh">获取某天所属周的开始和结束时间</para>
        /// </summary>
        /// <param name="firstDay">
        ///     <para xml:lang="en">The first day of the week (Sunday, Monday, etc.)</para>
        ///     <para xml:lang="zh">一周的第一天（周日、周一等）</para>
        /// </param>
        public ValueTuple<DateTime, DateTime> WeekStartEnd(DayOfWeek firstDay) => new(dt.WeekStart(firstDay), dt.WeekEnd(firstDay));

        /// <summary>
        ///     <para xml:lang="en">Gets the week number of a specific date within the year</para>
        ///     <para xml:lang="zh">获取某日期在一年中的周数</para>
        /// </summary>
        /// <param name="cultureInfo">
        ///     <para xml:lang="en">The culture info, default is the current culture</para>
        ///     <para xml:lang="zh">区域信息，默认是当前区域</para>
        /// </param>
        public int GetWeekOfYear(CultureInfo? cultureInfo = null)
        {
            var culture = cultureInfo ?? CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(dt, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        }

        /// <summary>
        ///     <para xml:lang="en">Converts a DateTime to the number of milliseconds since the Unix epoch</para>
        ///     <para xml:lang="zh">将日期时间转换为自 Unix 纪元以来的毫秒数</para>
        /// </summary>
        public long MillisecondsSinceEpoch=> (dt.ToUniversalTime() - DateTime.UnixEpoch).Ticks / 10000;

        /// <summary>
        ///     <para xml:lang="en">Converts a DateTime to the number of seconds since the Unix epoch</para>
        ///     <para xml:lang="zh">将日期时间转换为自 Unix 纪元以来的秒数</para>
        /// </summary>
        public long SecondsSinceEpoch=> (dt.ToUniversalTime() - DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerSecond;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the start and end time of a specific week by week number and year</para>
    ///     <para xml:lang="zh">根据周数和年份获取某周的开始和结束时间</para>
    /// </summary>
    /// <param name="week">
    ///     <para xml:lang="en">The week number within a year</para>
    ///     <para xml:lang="zh">一年中的周数</para>
    /// </param>
    /// <param name="year">
    ///     <para xml:lang="en">The year</para>
    ///     <para xml:lang="zh">年份</para>
    /// </param>
    /// <param name="firstDay">
    ///     <para xml:lang="en">The first day of the week (Sunday, Monday, etc.)</para>
    ///     <para xml:lang="zh">一周的第一天（周日、周一等）</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">(Start, End)</para>
    ///     <para xml:lang="zh">(开始时间, 结束时间)</para>
    /// </returns>
    public static ValueTuple<DateTime, DateTime> WeekStartEndByNumber(this int week, int year, DayOfWeek firstDay) => new DateTime(year, 1, 1).AddDays((week - 1) * 7).WeekStartEnd(firstDay);

    /// <summary>
    ///     <para xml:lang="en">Gets the start and end time of a specific month by month number and year</para>
    ///     <para xml:lang="zh">根据月份和年份获取某月的开始和结束时间</para>
    /// </summary>
    /// <param name="month">
    ///     <para xml:lang="en">The month number</para>
    ///     <para xml:lang="zh">月份</para>
    /// </param>
    /// <param name="year">
    ///     <para xml:lang="en">The year</para>
    ///     <para xml:lang="zh">年份</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">(Start, End)</para>
    ///     <para xml:lang="zh">(开始时间, 结束时间)</para>
    /// </returns>
    public static ValueTuple<DateTime, DateTime> MonthStartEndByMonth(this int month, int year) => (month < 1) | (month > 13) ? throw new("非法月份") : new DateTime(year, month, 2).MonthStartEnd;

    /// <summary>
    ///     <para xml:lang="en">Gets the numeric representation of a day of the week</para>
    ///     <para xml:lang="zh">获取一周中某天的数字表示</para>
    /// </summary>
    /// <param name="day">
    ///     <para xml:lang="en">The day of the week</para>
    ///     <para xml:lang="zh">一周中的某天</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The numeric representation of the day</para>
    ///     <para xml:lang="zh">该天的数字表示</para>
    /// </returns>
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
    ///     <para xml:lang="en">Converts a number (0-7) to a DayOfWeek</para>
    ///     <para xml:lang="zh">将数字（0-7）转换为 DayOfWeek</para>
    /// </summary>
    /// <param name="number">
    ///     <para xml:lang="en">The number to convert</para>
    ///     <para xml:lang="zh">要转换的数字</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The corresponding DayOfWeek</para>
    ///     <para xml:lang="zh">对应的 DayOfWeek</para>
    /// </returns>
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
    ///     <para xml:lang="en">Gets the Chinese name of a day of the week</para>
    ///     <para xml:lang="zh">获取一周中某天的中文名称</para>
    /// </summary>
    /// <param name="day">
    ///     <para xml:lang="en">The day of the week as an integer</para>
    ///     <para xml:lang="zh">一周中的某天，表示为整数</para>
    /// </param>
    /// <param name="type">
    ///     <para xml:lang="en">1 for "周", otherwise "星期"</para>
    ///     <para xml:lang="zh">1 表示 "周"，否则表示 "星期"</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The Chinese name of the day</para>
    ///     <para xml:lang="zh">该天的中文名称</para>
    /// </returns>
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
    ///     <para xml:lang="en">Gets the Chinese name of a day of the week</para>
    ///     <para xml:lang="zh">获取一周中某天的中文名称</para>
    /// </summary>
    /// <param name="day">
    ///     <para xml:lang="en">The day of the week</para>
    ///     <para xml:lang="zh">一周中的某天</para>
    /// </param>
    /// <param name="type">
    ///     <para xml:lang="en">1 for "周", otherwise "星期"</para>
    ///     <para xml:lang="zh">1 表示 "周"，否则表示 "星期"</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The Chinese name of the day</para>
    ///     <para xml:lang="zh">该天的中文名称</para>
    /// </returns>
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
    ///     <para xml:lang="en">Validates the overlap between two time periods</para>
    ///     <para xml:lang="zh">验证两个时间段的重合情况</para>
    /// </summary>
    /// <param name="sub">
    ///     <para xml:lang="en">The time period to validate</para>
    ///     <para xml:lang="zh">需要验证的时间段</para>
    /// </param>
    /// <param name="source">
    ///     <para xml:lang="en">The source time period</para>
    ///     <para xml:lang="zh">源时间段</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The overlap situation</para>
    ///     <para xml:lang="zh">重合情况</para>
    /// </returns>
    public static ETimeOverlap TimeOverlap(Tuple<DateTime, DateTime> sub, Tuple<DateTime, DateTime> source)
    {
        var (subStart, subEnd) = sub;
        var (validateStart, validateEnd) = source;
        return (subStart < validateEnd && validateStart < subEnd) switch
        {
            true when subStart >= validateStart && subEnd <= validateEnd                          => ETimeOverlap.完全重合,
            true when subStart < validateStart && subEnd >= validateStart && subEnd < validateEnd => ETimeOverlap.后段重合,
            true when subStart > validateStart && subStart < validateEnd && subEnd > validateEnd  => ETimeOverlap.前段重合,
            _                                                                                     => ETimeOverlap.完全不重合
        };
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets the number of weeks between a specific date and another date, returns -1 if the date to calculate is earlier than the
    ///     start date
    ///     </para>
    ///     <para xml:lang="zh">获取某个日期从另一个日期开始的间隔周数，当要计算的日期小于起始日期时，返回-1</para>
    /// </summary>
    /// <param name="point">
    ///     <para xml:lang="en">The start date</para>
    ///     <para xml:lang="zh">起始日期</para>
    /// </param>
    /// <param name="date">
    ///     <para xml:lang="en">The date to calculate</para>
    ///     <para xml:lang="zh">要计算的日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The number of weeks</para>
    ///     <para xml:lang="zh">间隔周数</para>
    /// </returns>
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
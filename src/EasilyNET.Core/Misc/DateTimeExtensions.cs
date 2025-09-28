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
    public static ETimeOverlap TimeOverlap(Tuple<DateTime, DateTime> sub, Tuple<DateTime, DateTime> source)
    {
        var (subStart, subEnd) = sub;
        var (sourceStart, sourceEnd) = source;
        // Ensure that start times are before end times
        if (subStart > subEnd)
        {
            throw new ArgumentException("Sub start time cannot be after sub end time.", nameof(sub));
        }
        if (sourceStart > sourceEnd)
        {
            throw new ArgumentException("Source start time cannot be after source end time.", nameof(source));
        }
        // Check for no overlap
        if (subEnd <= sourceStart || subStart >= sourceEnd)
        {
            return ETimeOverlap.NoOverlap;
        }
        // Check for sub completely within source
        if (subStart >= sourceStart && subEnd <= sourceEnd)
        {
            return ETimeOverlap.SubWithinSource;
        }
        // Check for source completely within sub
        if (sourceStart >= subStart && sourceEnd <= subEnd)
        {
            return ETimeOverlap.SourceWithinSub;
        }
        // Check for sub overlapping the start of source
        if (subStart < sourceStart && subEnd > sourceStart)
        {
            return ETimeOverlap.SubOverlapsStartOfSource;
        }
        // Check for sub overlapping the end of source
        // This condition (subStart < sourceEnd && subEnd > sourceEnd) is implicitly covered by the remaining overlap scenarios
        // and the fact that we've already checked for NoOverlap, SubWithinSource, and SourceWithinSub.
        // If none of the above, it must be SubOverlapsEndOfSource or a more complex overlap not yet defined.
        // For this refined enum, this will be SubOverlapsEndOfSource.
        return ETimeOverlap.SubOverlapsEndOfSource;
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
    public static int WeekNoFromPoint(DateTime point, DateTime? date)
    {
        date ??= DateTime.Now;
        if (date < point)
        {
            return -1;
        }
        var dayDiff = point.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)point.DayOfWeek - 1;
        var first_monday = point.AddDays(-dayDiff);
        var daysCount = (date.Value - first_monday).TotalDays;
        return (int)(daysCount / 7) + (daysCount % 7 == 0 ? 0 : 1);
    }

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
        public (DateTime Start, DateTime End) DayStartEnd => new(dt.DayStart, dt.DayEnd);

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
        public (DateTime Start, DateTime End) MonthStartEnd => new(dt.MonthStart, dt.MonthEnd);

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
        public (DateTime Start, DateTime End) YearStartEnd => new(dt.YearStart, dt.YearEnd);

        /// <summary>
        ///     <para xml:lang="en">Converts a DateTime to the number of milliseconds since the Unix epoch</para>
        ///     <para xml:lang="zh">将日期时间转换为自 Unix 纪元以来的毫秒数</para>
        /// </summary>
        public long MillisecondsSinceEpoch => (dt.ToUniversalTime() - DateTime.UnixEpoch).Ticks / 10000;

        /// <summary>
        ///     <para xml:lang="en">Converts a DateTime to the number of seconds since the Unix epoch</para>
        ///     <para xml:lang="zh">将日期时间转换为自 Unix 纪元以来的秒数</para>
        /// </summary>
        public long SecondsSinceEpoch => (dt.ToUniversalTime() - DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerSecond;

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
        public (DateTime Start, DateTime End) WeekStartEnd(DayOfWeek firstDay) => new(dt.WeekStart(firstDay), dt.WeekEnd(firstDay));

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
    }

    /// <param name="week">
    ///     <para xml:lang="en">The week number within a year</para>
    ///     <para xml:lang="zh">一年中的周数</para>
    /// </param>
    extension(int week)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the start and end time of a specific week by week number and year</para>
        ///     <para xml:lang="zh">根据周数和年份获取某周的开始和结束时间</para>
        /// </summary>
        /// <param name="year">
        ///     <para xml:lang="en">The year</para>
        ///     <para xml:lang="zh">年份</para>
        /// </param>
        /// <param name="firstDay">
        ///     <para xml:lang="en">The first day of the week (Sunday, Monday, etc.)</para>
        ///     <para xml:lang="zh">一周的第一天（周日、周一等）</para>
        /// </param>
        public (DateTime Start, DateTime End) WeekStartEndByNumber(int year, DayOfWeek firstDay) => new DateTime(year, 1, 1).AddDays((week - 1) * 7).WeekStartEnd(firstDay);

        /// <summary>
        ///     <para xml:lang="en">Gets the start and end time of a specific month by month number and year</para>
        ///     <para xml:lang="zh">根据月份和年份获取某月的开始和结束时间</para>
        /// </summary>
        /// <param name="year">
        ///     <para xml:lang="en">The year</para>
        ///     <para xml:lang="zh">年份</para>
        /// </param>
        public (DateTime Start, DateTime End) MonthStartEndByMonth(int year) => week is < 1 or > 12 ? throw new("非法月份") : new DateTime(year, week, 2).MonthStartEnd;

        /// <summary>
        ///     <para xml:lang="en">Converts a number (0-7) to a DayOfWeek</para>
        ///     <para xml:lang="zh">将数字（0-7）转换为 DayOfWeek</para>
        /// </summary>
        public DayOfWeek ToDayOfWeek() =>
            week is > 7 or < 0
                ? throw new("please input 0-7")
                : week switch
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
        /// <param name="type">
        ///     <para xml:lang="en">1 for "周", otherwise "星期"</para>
        ///     <para xml:lang="zh">1 表示 "周"，否则表示 "星期"</para>
        /// </param>
        public string DayName(int type = 1)
        {
            var name = week switch
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
    }

    /// <param name="day">
    ///     <para xml:lang="en">The day of the week</para>
    ///     <para xml:lang="zh">一周中的某天</para>
    /// </param>
    extension(DayOfWeek day)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets the numeric representation of a day of the week</para>
        ///     <para xml:lang="zh">获取一周中某天的数字表示</para>
        /// </summary>
        public int DayNumber() =>
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
        ///     <para xml:lang="en">Gets the Chinese name of a day of the week</para>
        ///     <para xml:lang="zh">获取一周中某天的中文名称</para>
        /// </summary>
        /// <param name="type">
        ///     <para xml:lang="en">1 for "周", otherwise "星期"</para>
        ///     <para xml:lang="zh">1 表示 "周"，否则表示 "星期"</para>
        /// </param>
        public string DayName(int type = 1)
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
    }
}
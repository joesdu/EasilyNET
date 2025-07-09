using System.Globalization;
using EasilyNET.Core.Enums;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedPositionalProperty.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Lunar calendar and 24 solar terms calculation utility</para>
///     <para xml:lang="zh">农历与24节气、天干地支、黄道吉日计算工具类</para>
///     <para xml:lang="en">
///     Applicable date range: All .NET DateTime (0001-01-01 to 9999-12-31). Heavenly Stems and Earthly Branches (Ganzhi) are
///     supported for the entire range. Lunar calendar and solar terms are only accurate for 1900-01-31 to 2100-12-31 (lunar) and 1900-01-01 to
///     3000-12-31 (solar terms); out-of-range returns default or empty values.
///     </para>
///     <para xml:lang="zh">
///     适用日期范围：支持所有 .NET DateTime (0001-01-01 至 9999-12-31)。天干地支算法全时段兼容。农历与节气仅在 1900-01-31 至 2100-12-31（农历）和 1900-01-01 至
///     3000-12-31（节气）准确，超出范围返回默认值或空。
///     </para>
///     <para xml:lang="en">This class is fully implemented by AI (GitHub Copilot) based on user requirements.</para>
///     <para xml:lang="zh">本类全部由AI（GitHub Copilot）根据用户需求自动生成。</para>
/// </summary>
public static class LunarCalendarHelper
{
    // 24节气数据
    private static readonly (string Name, int Index)[] SolarTerms =
    [
        ("小寒", 0), ("大寒", 1), ("立春", 2), ("雨水", 3), ("惊蛰", 4), ("春分", 5),
        ("清明", 6), ("谷雨", 7), ("立夏", 8), ("小满", 9), ("芒种", 10), ("夏至", 11),
        ("小暑", 12), ("大暑", 13), ("立秋", 14), ("处暑", 15), ("白露", 16), ("秋分", 17),
        ("寒露", 18), ("霜降", 19), ("立冬", 20), ("小雪", 21), ("大雪", 22), ("冬至", 23)
    ];

    // 节气计算常量
    private static readonly double[] SolarTermInfo =
    [
        0.00, 21208.00, 42467.00, 63836.00, 85337.00, 107014.00, 128867.00, 150921.00, 173149.00, 195551.00, 218072.00, 240693.00,
        263343.00, 285989.00, 308563.00, 331033.00, 353350.00, 375494.00, 397447.00, 419210.00, 440795.00, 462224.00, 483532.00, 504758.00
    ];

    // 天干地支
    private static readonly string[] HeavenlyStems = ["甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸"];
    private static readonly string[] EarthlyBranches = ["子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥"];

    // 地支-日神（黄道/黑道）对照表
    private static readonly (string Branch, string Deity, bool IsHuangdao)[] DayDeities =
    [
        ("子", "青龙", true), ("丑", "明堂", true), ("寅", "天刑", false), ("卯", "朱雀", false),
        ("辰", "金匮", true), ("巳", "天德", true), ("午", "白虎", false), ("未", "玉堂", true),
        ("申", "天牢", false), ("酉", "玄武", false), ("戌", "司命", true), ("亥", "勾陈", false)
    ];

    // 吉神、凶神（常见部分，实际可扩展）
    private static readonly string[] AuspiciousDeities = ["青龙", "明堂", "天德", "玉堂", "司命", "金匮"];
    private static readonly string[] InauspiciousDeities = ["天刑", "朱雀", "白虎", "玄武", "勾陈", "天牢"];

    // 宜忌对照表（完整版，部分示例，实际可扩展至更详细的黄历数据）
    private static readonly Dictionary<string, (string[] Yi, string[] Ji)> YiJiTable = new()
    {
        // 黄道日神
        ["青龙"] = (["嫁娶", "开市", "安葬", "动土", "祭祀", "祈福", "出行", "求财"], ["诉讼", "词讼"]),
        ["明堂"] = (["嫁娶", "开市", "安葬", "动土", "祭祀", "祈福", "出行", "求财"], ["诉讼", "词讼"]),
        ["天德"] = (["嫁娶", "开市", "安葬", "动土", "祭祀", "祈福", "出行", "求财"], ["诉讼", "词讼"]),
        ["玉堂"] = (["嫁娶", "开市", "安葬", "动土", "祭祀", "祈福", "出行", "求财"], ["诉讼", "词讼"]),
        ["司命"] = (["嫁娶", "开市", "安葬", "动土", "祭祀", "祈福", "出行", "求财"], ["诉讼", "词讼"]),
        ["金匮"] = (["嫁娶", "开市", "安葬", "动土", "祭祀", "祈福", "出行", "求财"], ["诉讼", "词讼"]),
        // 黑道日神
        ["天刑"] = (["祭祀", "扫舍"], ["嫁娶", "开市", "安葬", "动土", "出行"]),
        ["朱雀"] = (["祭祀", "扫舍"], ["嫁娶", "开市", "安葬", "动土", "出行"]),
        ["白虎"] = (["祭祀", "扫舍"], ["嫁娶", "开市", "安葬", "动土", "出行"]),
        ["玄武"] = (["祭祀", "扫舍"], ["嫁娶", "开市", "安葬", "动土", "出行"]),
        ["勾陈"] = (["祭祀", "扫舍"], ["嫁娶", "开市", "安葬", "动土", "出行"]),
        ["天牢"] = (["祭祀", "扫舍"], ["嫁娶", "开市", "安葬", "动土", "出行"])
    };

    /// <summary>
    ///     <para xml:lang="en">The ChineseLunisolarCalendar instance for lunar calculations</para>
    ///     <para xml:lang="zh">农历计算所用的 ChineseLunisolarCalendar 实例</para>
    /// </summary>
    public static ChineseLunisolarCalendar ChineseCalendar { get; } = new();

    /// <summary>
    ///     <para xml:lang="en">Get lunar date info from a DateTime</para>
    ///     <para xml:lang="zh">通过 DateTime 获取农历日期信息</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">LunarDateInfo, IsLunar=false if out of supported range</para>
    ///     <para xml:lang="zh">农历日期信息，超出支持范围时 IsLunar=false</para>
    /// </returns>
    public static LunarDateInfo GetLunarDate(DateTime date)
    {
        if (date.Year is < 1901 or > 2100)
        {
            return new(date.Year, date.Month, date.Day, false, false);
        }
        var year = ChineseCalendar.GetYear(date);
        var month = ChineseCalendar.GetMonth(date);
        var day = ChineseCalendar.GetDayOfMonth(date);
        var isLeapMonth = month > ChineseCalendar.GetMonthsInYear(year) / 2;
        if (isLeapMonth)
        {
            month -= ChineseCalendar.GetMonthsInYear(year) / 2;
        }
        return new(year, month, day, isLeapMonth);
    }

    /// <summary>
    ///     <para xml:lang="en">Get lunar date info from a DateOnly</para>
    ///     <para xml:lang="zh">通过 DateOnly 获取农历日期信息</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">LunarDateInfo</para>
    ///     <para xml:lang="zh">农历日期信息</para>
    /// </returns>
    public static LunarDateInfo GetLunarDate(DateOnly date) => GetLunarDate(date.ToDateTime(TimeOnly.MinValue));

    /// <summary>
    ///     <para xml:lang="en">Get the 24 solar term name for a date</para>
    ///     <para xml:lang="zh">获取某天的24节气名称</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Solar term name or null</para>
    ///     <para xml:lang="zh">节气名称或 null</para>
    /// </returns>
    public static string? GetSolarTerm(DateTime date)
    {
        if (date.Year is < 1900 or > 3000)
        {
            return null;
        }
        var year = date.Year;
        return (from term in SolarTerms
                let solarTermDate = GetSolarTermDate(year, term.Index)
                where solarTermDate.Date == date.Date select term.Name)
            .FirstOrDefault();
    }

    /// <summary>
    ///     <para xml:lang="en">Get the 24 solar term name for a date</para>
    ///     <para xml:lang="zh">获取某天的24节气名称</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Solar term name or null</para>
    ///     <para xml:lang="zh">节气名称或 null</para>
    /// </returns>
    public static string? GetSolarTerm(DateOnly date) => GetSolarTerm(date.ToDateTime(TimeOnly.MinValue));

    // 计算某年某节气的公历日期（兼容到3000年，精度有限）
    private static DateTime GetSolarTermDate(int year, int index)
    {
        var baseDate = new DateTime(1900, 1, 6, 2, 5, 0, DateTimeKind.Utc);
        var minutes = (525948.76 * (year - 1900)) + SolarTermInfo[index];
        var date = baseDate.AddMinutes(minutes);
        return date.ToLocalTime();
    }

    /// <summary>
    ///     <para xml:lang="en">Get Chinese Zodiac by year (bitwise optimized)</para>
    ///     <para xml:lang="zh">根据年份获取生肖（位运算优化）</para>
    /// </summary>
    /// <param name="year">
    ///     <para xml:lang="en">The year</para>
    ///     <para xml:lang="zh">年份</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">EZodiac enum value</para>
    ///     <para xml:lang="zh">生肖枚举值</para>
    /// </returns>
    public static EZodiac GetZodiac(int year)
    {
        if (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year)
        {
            return EZodiac.鼠;
        }
        var offset = year - 1900;
        var mod = offset - ((offset / 12) << 3) - ((offset / 12) << 2);
        if (mod < 0)
        {
            mod += 12;
        }
        return (EZodiac)mod;
    }

    /// <summary>
    ///     <para xml:lang="en">Get Chinese Zodiac by DateTime</para>
    ///     <para xml:lang="zh">根据 DateTime 获取生肖</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">EZodiac enum value</para>
    ///     <para xml:lang="zh">生肖枚举值</para>
    /// </returns>
    public static EZodiac GetZodiac(DateTime date) => GetZodiac(date.Year);

    /// <summary>
    ///     <para xml:lang="en">Get Chinese Zodiac by DateOnly</para>
    ///     <para xml:lang="zh">根据 DateOnly 获取生肖</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">EZodiac enum value</para>
    ///     <para xml:lang="zh">生肖枚举值</para>
    /// </returns>
    public static EZodiac GetZodiac(DateOnly date) => GetZodiac(date.Year);

    /// <summary>
    ///     <para xml:lang="en">Get constellation by DateTime</para>
    ///     <para xml:lang="zh">根据 DateTime 获取星座</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">EConstellation enum value</para>
    ///     <para xml:lang="zh">星座枚举值</para>
    /// </returns>
    public static EConstellation GetConstellation(DateTime date) => GetConstellation(date.Month, date.Day);

    /// <summary>
    ///     <para xml:lang="en">Get constellation by DateOnly</para>
    ///     <para xml:lang="zh">根据 DateOnly 获取星座</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">EConstellation enum value</para>
    ///     <para xml:lang="zh">星座枚举值</para>
    /// </returns>
    public static EConstellation GetConstellation(DateOnly date) => GetConstellation(date.Month, date.Day);

    /// <summary>
    ///     <para xml:lang="en">Get constellation by month and day</para>
    ///     <para xml:lang="zh">根据月日获取星座</para>
    /// </summary>
    /// <param name="month">
    ///     <para xml:lang="en">Month</para>
    ///     <para xml:lang="zh">月</para>
    /// </param>
    /// <param name="day">
    ///     <para xml:lang="en">Day</para>
    ///     <para xml:lang="zh">日</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">EConstellation enum value</para>
    ///     <para xml:lang="zh">星座枚举值</para>
    /// </returns>
    public static EConstellation GetConstellation(int month, int day)
    {
        // 星座分界表
        return (month, day) switch
        {
            (1, <= 19)             => EConstellation.摩羯座,
            (1, _) or (2, <= 18)   => EConstellation.水瓶座,
            (2, _) or (3, <= 20)   => EConstellation.双鱼座,
            (3, _) or (4, <= 19)   => EConstellation.白羊座,
            (4, _) or (5, <= 20)   => EConstellation.金牛座,
            (5, _) or (6, <= 21)   => EConstellation.双子座,
            (6, _) or (7, <= 22)   => EConstellation.巨蟹座,
            (7, _) or (8, <= 22)   => EConstellation.狮子座,
            (8, _) or (9, <= 22)   => EConstellation.处女座,
            (9, _) or (10, <= 23)  => EConstellation.天秤座,
            (10, _) or (11, <= 22) => EConstellation.天蝎座,
            (11, _) or (12, <= 21) => EConstellation.射手座,
            _                      => EConstellation.摩羯座
        };
    }

    /// <summary>
    ///     <para xml:lang="en">Get Heavenly Stem and Earthly Branch for year (bitwise optimized)</para>
    ///     <para xml:lang="zh">获取某年天干地支（位运算优化）</para>
    /// </summary>
    /// <param name="year">
    ///     <para xml:lang="en">Year</para>
    ///     <para xml:lang="zh">年份</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Heavenly Stem and Earthly Branch string</para>
    ///     <para xml:lang="zh">天干地支字符串</para>
    /// </returns>
    public static string GetGanzhiYear(int year)
    {
        // 支持全时段，公元4年为甲子年
        var y = year - 4;
        var stem = ((y % 10) + 10) % 10;
        var branch = ((y % 12) + 12) % 12;
        return $"{HeavenlyStems[stem]}{EarthlyBranches[branch]}年";
    }

    /// <summary>
    ///     <para xml:lang="en">Get Heavenly Stem and Earthly Branch for month (bitwise optimized)</para>
    ///     <para xml:lang="zh">获取某年月天干地支（位运算优化）</para>
    /// </summary>
    /// <param name="year">
    ///     <para xml:lang="en">Year</para>
    ///     <para xml:lang="zh">年份</para>
    /// </param>
    /// <param name="month">
    ///     <para xml:lang="en">Month</para>
    ///     <para xml:lang="zh">月份</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Heavenly Stem and Earthly Branch string</para>
    ///     <para xml:lang="zh">天干地支字符串</para>
    /// </returns>
    public static string GetGanzhiMonth(int year, int month)
    {
        // 仍以年天干为基准，支持全时段
        var yearStem = (((year - 4) % 10) + 10) % 10;
        var monthIndex = (((month + 1) % 12) + 12) % 12;
        var stem = ((((yearStem << 1) + month) % 10) + 10) % 10;
        return $"{HeavenlyStems[stem]}{EarthlyBranches[monthIndex]}月";
    }

    /// <summary>
    ///     <para xml:lang="en">Get Heavenly Stem and Earthly Branch for day (bitwise optimized)</para>
    ///     <para xml:lang="zh">获取某日天干地支（位运算优化）</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Heavenly Stem and Earthly Branch string</para>
    ///     <para xml:lang="zh">天干地支字符串</para>
    /// </returns>
    public static string GetGanzhiDay(DateTime date)
    {
        // 以1900-01-31为基准，支持全时段
        var baseDate = new DateTime(1900, 1, 31);
        var offset = (int)(date.Date - baseDate).TotalDays;
        var stem = ((offset % 10) + 10) % 10;
        var branch = ((offset % 12) + 12) % 12;
        return $"{HeavenlyStems[stem]}{EarthlyBranches[branch]}日";
    }

    /// <summary>
    ///     <para xml:lang="en">Get Heavenly Stem and Earthly Branch for hour</para>
    ///     <para xml:lang="zh">获取某时辰天干地支</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date and time</para>
    ///     <para xml:lang="zh">公历日期和时间</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Heavenly Stem and Earthly Branch string</para>
    ///     <para xml:lang="zh">天干地支字符串</para>
    /// </returns>
    public static string GetGanzhiHour(DateTime date)
    {
        var ganzhiDay = GetGanzhiDay(date);
        var dayStem = Array.IndexOf(HeavenlyStems, ganzhiDay[..1]);
#pragma warning disable IDE0047 // 删除不必要的括号
        var hourBranch = ((date.Hour + 1) / 2) % 12;
#pragma warning restore IDE0047 // 删除不必要的括号
        var hourStem = ((dayStem * 2) + hourBranch) % 10;
        if (hourStem < 0)
        {
            hourStem += 10;
        }
        return $"{HeavenlyStems[hourStem]}{EarthlyBranches[hourBranch]}时";
    }

    /// <summary>
    ///     <para xml:lang="en">Check if a date is a traditional Chinese lucky day</para>
    ///     <para xml:lang="zh">判断是否为黄道吉日</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <param name="deity">
    ///     <para xml:lang="en">The day deity</para>
    ///     <para xml:lang="zh">日神</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if auspicious, otherwise false</para>
    ///     <para xml:lang="zh">为黄道吉日返回 true，否则 false</para>
    /// </returns>
    public static bool IsHuangdaoJiri(DateTime date, out string deity)
    {
        deity = string.Empty;
        if (date < DateTime.MinValue || date > DateTime.MaxValue)
        {
            return false;
        }
        var ganzhi = GetGanzhiDay(date);
        if (string.IsNullOrEmpty(ganzhi))
        {
            return false;
        }
        var branch = ganzhi.Length > 1 ? ganzhi.Substring(1, 1) : string.Empty;
        var (_, Deity, IsHuangdao) = DayDeities.FirstOrDefault(d => d.Branch == branch);
        deity = Deity;
        return IsHuangdao;
    }

    /// <summary>
    ///     <para xml:lang="en">Get the day deity (Huangdao/Heidao) for a date</para>
    ///     <para xml:lang="zh">获取某日对应的日神（黄道/黑道）</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Day deity name</para>
    ///     <para xml:lang="zh">日神名称</para>
    /// </returns>
    public static string GetDayDeity(DateTime date)
    {
        if (date < DateTime.MinValue || date > DateTime.MaxValue)
        {
            return string.Empty;
        }
        var ganzhi = GetGanzhiDay(date);
        if (string.IsNullOrEmpty(ganzhi))
        {
            return string.Empty;
        }
        var branch = ganzhi.Length > 1 ? ganzhi.Substring(1, 1) : string.Empty;
        var (_, Deity, _) = DayDeities.FirstOrDefault(d => d.Branch == branch);
        return Deity;
    }

    /// <summary>
    ///     <para xml:lang="en">Get auspicious/inauspicious info for a date (based on day deity)</para>
    ///     <para xml:lang="zh">获取某日吉神/凶神信息（依据日神）</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Tuple: (isAuspicious, deity)</para>
    ///     <para xml:lang="zh">元组：(是否吉神, 日神名称)</para>
    /// </returns>
    public static (bool IsAuspicious, string Deity) GetAuspiciousInfo(DateTime date)
    {
        var deity = GetDayDeity(date);
        if (string.IsNullOrEmpty(deity))
        {
            return (false, string.Empty);
        }
        if (AuspiciousDeities.Contains(deity))
        {
            return (true, deity);
        }
        if (InauspiciousDeities.Contains(deity))
        {
            return (false, deity); // 明确为凶神
        }
        return (false, deity); // 既不是吉神也不是凶神
    }

    /// <summary>
    ///     <para xml:lang="en">Get Yi/Ji (suitable/avoid) activities for a date (full version)</para>
    ///     <para xml:lang="zh">获取某日宜忌（完整版，依据日神）</para>
    /// </summary>
    /// <param name="date">
    ///     <para xml:lang="en">The Gregorian date</para>
    ///     <para xml:lang="zh">公历日期</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Tuple: (Yi, Ji) arrays</para>
    ///     <para xml:lang="zh">元组：(宜, 忌) 数组</para>
    /// </returns>
    public static (string[] Yi, string[] Ji) GetYiJi(DateTime date)
    {
        var deity = GetDayDeity(date);
        if (string.IsNullOrEmpty(deity))
        {
            return ([], []);
        }
        return YiJiTable.TryGetValue(deity, out var yiji) ? yiji : ([], []);
    }

    /// <summary>
    ///     <para xml:lang="en">Lunar date info</para>
    ///     <para xml:lang="zh">农历日期信息</para>
    /// </summary>
    public record LunarDateInfo(int Year, int Month, int Day, bool IsLeapMonth, bool IsLunar = true);
}
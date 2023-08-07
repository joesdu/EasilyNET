namespace EasilyNET.Core.Timing;

/// <summary>
/// 系统时间
/// </summary>
public static class SystemTime
{
    /// <summary>
    ///
    /// </summary>
    public static Func<DateTime> Now = () => DateTime.UtcNow;

    /// <summary>
    /// 正常化
    /// </summary>
    public static Func<DateTime, DateTime> Normalize = dateTime => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
}
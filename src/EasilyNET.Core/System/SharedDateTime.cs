namespace GitLogMan;

/// <summary>
/// 共享的DateTime,用于在跨异步上下文中保持Now的值一致
/// </summary>
internal static class SharedDateTime
{
    private static readonly AsyncLocal<DateTime> _asyncLocalSharedNow = new();

    // 初始化 AsyncLocal 的值
    static SharedDateTime() => _asyncLocalSharedNow.Value = DateTime.Now;

    // 刷新当前线程中的共享 DateTime.Now 值
    public static void Refresh() => _asyncLocalSharedNow.Value = DateTime.Now;
    /// <summary>
    /// SharedNow
    /// </summary>
    public static DateTime SharedNow => _asyncLocalSharedNow.Value;
}
namespace EasilyNET.Core.System;

/// <summary>
/// 共享的DateTime,用于在跨异步上下文中保持Now的值一致
/// </summary>
public static class SharedDateTime
{
    private static readonly AsyncLocal<DateTime> _asyncLocalSharedNow = new();

    // 初始化 AsyncLocal 的值
    static SharedDateTime() => _asyncLocalSharedNow.Value = DateTime.Now;

    /// <summary>
    /// 刷新共享 DateTime.Now 值
    /// </summary>
    public static void Refresh() => _asyncLocalSharedNow.Value = DateTime.Now;
    
    /// <summary>
    /// SharedNow
    /// </summary>
    public static DateTime SharedNow => _asyncLocalSharedNow.Value;
}

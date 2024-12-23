// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.System;

/// <summary>
///     <para xml:lang="en">Shared DateTime, used to keep the value of Now consistent across asynchronous contexts</para>
///     <para xml:lang="zh">共享的 DateTime，用于在跨异步上下文中保持 Now 的值一致</para>
/// </summary>
public static class SharedDateTime
{
    private static readonly AsyncLocal<DateTime> _asyncLocalSharedNow = new();

    // 初始化 AsyncLocal 的值
    static SharedDateTime()
    {
        _asyncLocalSharedNow.Value = DateTime.Now;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the shared DateTime.Now value</para>
    ///     <para xml:lang="zh">获取共享的 DateTime.Now 值</para>
    /// </summary>
    public static DateTime SharedNow => _asyncLocalSharedNow.Value;

    /// <summary>
    ///     <para xml:lang="en">Refreshes the shared DateTime.Now value</para>
    ///     <para xml:lang="zh">刷新共享的 DateTime.Now 值</para>
    /// </summary>
    public static void Refresh() => _asyncLocalSharedNow.Value = DateTime.Now;
}
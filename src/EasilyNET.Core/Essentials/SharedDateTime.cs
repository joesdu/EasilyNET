// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Essentials;

/// <summary>
///     <para xml:lang="en">Shared DateTime, used to keep the value of Now consistent across asynchronous contexts</para>
///     <para xml:lang="zh">共享的 DateTime，用于在跨异步上下文中保持 Now 的值一致</para>
/// </summary>
public static class SharedDateTime
{
    private static readonly AsyncLocal<DateTime?> _asyncLocalSharedNow = new();

    /// <summary>
    ///     <para xml:lang="en">Gets the shared DateTime.Now value. If not set, returns current DateTime.Now and sets it for this context.</para>
    ///     <para xml:lang="zh">获取共享的 DateTime.Now 值。如果未设置，则返回当前 DateTime.Now 并为此上下文设置。</para>
    /// </summary>
    public static DateTime SharedNow
    {
        get
        {
            _asyncLocalSharedNow.Value ??= DateTime.Now;
            return _asyncLocalSharedNow.Value.Value;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Refreshes the shared DateTime.Now value</para>
    ///     <para xml:lang="zh">刷新共享的 DateTime.Now 值</para>
    /// </summary>
    public static void Refresh() => _asyncLocalSharedNow.Value = DateTime.Now;

    /// <summary>
    ///     <para xml:lang="en">Sets the shared DateTime value manually (for testing or custom scenarios)</para>
    ///     <para xml:lang="zh">手动设置共享的 DateTime 值（用于测试或自定义场景）</para>
    /// </summary>
    public static void Set(DateTime dateTime) => _asyncLocalSharedNow.Value = dateTime;

    /// <summary>
    ///     <para xml:lang="en">Resets the shared DateTime value so next access will use DateTime.Now</para>
    ///     <para xml:lang="zh">重置共享的 DateTime 值，下次访问时会使用当前时间</para>
    /// </summary>
    public static void Reset() => _asyncLocalSharedNow.Value = null;
}
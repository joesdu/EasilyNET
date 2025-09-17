using System.Collections.Concurrent;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 缓存管理器，统一管理所有缓存
/// </summary>
internal sealed class CacheManager
{
    public ConcurrentDictionary<Type, List<Type>> EventHandlerCache { get; } = [];

    /// <summary>
    /// 清除事件处理器相关缓存
    /// </summary>
    public void ClearEventHandlerCaches()
    {
        EventHandlerCache.Clear();
    }
}
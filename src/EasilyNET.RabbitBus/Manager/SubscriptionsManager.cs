using EasilyNET.RabbitBus.Abstraction;
using System.Collections.Concurrent;

namespace EasilyNET.RabbitBus;

/// <summary>
/// RabbitMQ订阅管理器
/// </summary>
internal sealed class SubscriptionsManager : ISubscriptionsManager
{
    private readonly ConcurrentDictionary<string, List<Type>> _handlers = new();

    /// <summary>
    /// 添加订阅
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="handlerType"></param>
    public void AddSubscription(Type eventType, Type handlerType)
    {
        var eventKey = GetEventKey(eventType);
        DoAddSubscription(handlerType, eventKey);
    }

    /// <summary>
    /// 从事件获取事件处理器
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public IEnumerable<Type> GetHandlersForEvent(string eventName) => _handlers[eventName];

    /// <summary>
    /// 是否有事件订阅
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    /// <summary>
    /// 清空
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
    }

    /// <summary>
    /// 获取事件名
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public string GetEventKey(Type type) => type.Name;

    private void DoAddSubscription(Type handlerType, string eventName)
    {
        if (!HasSubscriptionsForEvent(eventName)) _ = _handlers.TryAdd(eventName, new());
        if (_handlers[eventName].Any(o => o == handlerType))
            throw new ArgumentException($"类型:{handlerType.Name} 已注册 '{eventName}'", nameof(handlerType));
        _handlers[eventName].Add(handlerType);
    }
}
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using System.Collections.Concurrent;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// RabbitMQ订阅管理器
/// </summary>
internal sealed class SubscriptionsManager : ISubscriptionsManager
{
    private readonly ConcurrentDictionary<string, List<Type>> _handlers = new();

    /// <inheritdoc />
    public void AddSubscription(Type eventType, Type handlerType)
    {
        var eventKey = GetEventKey(eventType);
        DoAddSubscription(handlerType, eventKey);
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetHandlersForEvent(string eventName) => _handlers[eventName];

    /// <inheritdoc />
    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    /// <inheritdoc />
    public void Clear() => _handlers.Clear();

    /// <inheritdoc />
    public string GetEventKey(Type type) => type.Name;

    private void DoAddSubscription(Type handlerType, string eventName)
    {
        if (!HasSubscriptionsForEvent(eventName)) _ = _handlers.TryAdd(eventName, []);
        if (_handlers[eventName].Any(o => o == handlerType))
            throw new ArgumentException($"类型:{handlerType.Name} 已注册 '{eventName}'", nameof(handlerType));
        _handlers[eventName].Add(handlerType);
    }
}
using EasilyNET.Core.Misc;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using System.Collections.Concurrent;
using System.Reflection;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <inheritdoc />
internal sealed class SubscriptionsManager : ISubscriptionsManager
{
    private readonly ConcurrentDictionary<string, IList<Type>> _dlxHandlers = new();
    private readonly ConcurrentDictionary<string, IList<Type>> _handlers = new();

    /// <inheritdoc />
    public void AddSubscription(Type eventType, bool isDlx, IList<TypeInfo> handlerType) => DoAddSubscription(eventType.Name, isDlx, handlerType);

    /// <inheritdoc />
    public IEnumerable<Type> GetHandlersForEvent(string name, bool isDlx) => isDlx ? _dlxHandlers[name] : _handlers[name];

    /// <inheritdoc />
    public bool HasSubscriptionsForEvent(string name, bool isDlx) => isDlx ? _dlxHandlers.ContainsKey(name) : _handlers.ContainsKey(name);

    /// <inheritdoc />
    public void Clear()
    {
        _handlers.Clear();
        _dlxHandlers.Clear();
    }

    /// <inheritdoc />
    public string GetEventKey(Type type) => type.Name;

    private void DoAddSubscription(string name, bool isDlx, IList<TypeInfo> handlerType)
    {
        if (HasSubscriptionsForEvent(name, isDlx)) return;
        if (isDlx)
        {
            _dlxHandlers.TryAdd(name, []);
            if (_dlxHandlers[name].Any(handlerType.Contains))
                throw new ArgumentException($"类型已注册 '{name}'", nameof(handlerType));
            _dlxHandlers[name].AddRange(handlerType);
        }
        else
        {
            _ = _handlers.TryAdd(name, []);
            if (_handlers[name].Any(handlerType.Contains))
                throw new ArgumentException($"类型已注册 '{name}'", nameof(handlerType));
            _handlers[name].AddRange(handlerType);
        }
    }
}
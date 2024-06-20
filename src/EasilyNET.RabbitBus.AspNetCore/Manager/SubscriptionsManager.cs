using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using System.Collections.Concurrent;
using System.Reflection;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <inheritdoc />
internal sealed class SubscriptionsManager : ISubscriptionsManager
{
    private readonly ConcurrentDictionary<string, IList<Type>> _dlxHandlers = [];
    private readonly ConcurrentDictionary<string, IList<Type>> _handlers = [];

    /// <inheritdoc />
    public void AddSubscription(Type eventType, bool isDlx, IList<TypeInfo> handlerTypes)
    {
        var handlerTypeList = handlerTypes.Select(ht => ht.AsType()).ToList();
        DoAddSubscription(eventType.Name, isDlx, handlerTypeList);
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetHandlersForEvent(string name, bool isDlx) => isDlx ? _dlxHandlers.GetValueOrDefault(name, []) : _handlers.GetValueOrDefault(name, []);

    /// <inheritdoc />
    public bool HasSubscriptionsForEvent(string name, bool isDlx) => isDlx ? _dlxHandlers.ContainsKey(name) : _handlers.ContainsKey(name);

    /// <inheritdoc />
    public void Clear()
    {
        _handlers.Clear();
        _dlxHandlers.Clear();
    }

    private void DoAddSubscription(string name, bool isDlx, IList<Type> handlerTypes)
    {
        var handlersDict = isDlx ? _dlxHandlers : _handlers;
        if (!handlersDict.ContainsKey(name))
        {
            handlersDict.TryAdd(name, []);
        }
        var handlers = handlersDict[name];
        foreach (var handlerType in handlerTypes)
        {
            if (handlers.Contains(handlerType))
            {
                throw new($"Handler type already registered for '{name}'");
            }
            handlers.Add(handlerType);
        }
    }
}
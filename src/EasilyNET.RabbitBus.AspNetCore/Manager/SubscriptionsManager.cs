using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Enums;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <inheritdoc />
internal sealed class SubscriptionsManager : ISubscriptionsManager
{
    private readonly ConcurrentDictionary<string, IList<Type>> _delayedHandlers = [];
    private readonly ConcurrentDictionary<string, IList<Type>> _normalHandlers = [];

    public void AddSubscription(Type eventType, EKindOfHandler handleKind, IList<TypeInfo> handlerTypes)
    {
        var handlerTypeList = handlerTypes.Select(ht => ht.AsType()).ToList();
        DoAddSubscription(eventType.Name, handleKind, handlerTypeList);
    }

    public IEnumerable<Type> GetHandlersForEvent(string name, EKindOfHandler handleKind)
    {
        return handleKind switch
        {
            EKindOfHandler.Normal  => _normalHandlers.GetValueOrDefault(name, []),
            EKindOfHandler.Delayed => _delayedHandlers.GetValueOrDefault(name, []),
            _                      => throw new ArgumentOutOfRangeException(nameof(handleKind), handleKind, null)
        };
    }

    public bool HasSubscriptionsForEvent(string name, EKindOfHandler handleKind)
    {
        return handleKind switch
        {
            EKindOfHandler.Normal  => _normalHandlers.ContainsKey(name),
            EKindOfHandler.Delayed => _delayedHandlers.ContainsKey(name),
            _                      => throw new ArgumentOutOfRangeException(nameof(handleKind), handleKind, null)
        };
    }

    public void ClearSubscriptions()
    {
        _normalHandlers.Clear();
        _delayedHandlers.Clear();
    }

    private void DoAddSubscription(string name, EKindOfHandler handleKind, IList<Type> handlerTypes)
    {
        var handlersDict = handleKind switch
        {
            EKindOfHandler.Normal  => _normalHandlers,
            EKindOfHandler.Delayed => _delayedHandlers,
            _                      => throw new ArgumentOutOfRangeException(nameof(handleKind), handleKind, null)
        };
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
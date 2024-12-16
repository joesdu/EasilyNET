using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Enums;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <inheritdoc />
internal sealed class SubscriptionsManager : ISubscriptionsManager
{
    private readonly ConcurrentDictionary<string, HashSet<Type>> _delayedHandlers = new();
    private readonly ConcurrentDictionary<string, HashSet<Type>> _normalHandlers = new();

    public void AddSubscription(Type eventType, EKindOfHandler handleKind, IList<TypeInfo> handlerTypes)
    {
        var handlerTypeList = handlerTypes.Select(ht => ht.AsType()).ToList();
        DoAddSubscription(eventType.Name, handleKind, handlerTypeList);
    }

    public IEnumerable<Type> GetHandlersForEvent(string name, EKindOfHandler handleKind)
    {
        return handleKind switch
        {
            EKindOfHandler.Normal => _normalHandlers.TryGetValue(name, out var normalHandlers) ? normalHandlers : Enumerable.Empty<Type>(),
            EKindOfHandler.Delayed => _delayedHandlers.TryGetValue(name, out var delayedHandlers) ? delayedHandlers : Enumerable.Empty<Type>(),
            _ => throw new ArgumentOutOfRangeException(nameof(handleKind), handleKind, null)
        };
    }

    public bool HasSubscriptionsForEvent(string name, EKindOfHandler handleKind)
    {
        return handleKind switch
        {
            EKindOfHandler.Normal => _normalHandlers.ContainsKey(name),
            EKindOfHandler.Delayed => _delayedHandlers.ContainsKey(name),
            _ => throw new ArgumentOutOfRangeException(nameof(handleKind), handleKind, null)
        };
    }

    public void ClearSubscriptions()
    {
        _normalHandlers.Clear();
        _delayedHandlers.Clear();
    }

    [SuppressMessage("Style", "IDE0046:转换为条件表达式", Justification = "<挂起>")]
    private void DoAddSubscription(string name, EKindOfHandler handleKind, IList<Type> handlerTypes)
    {
        var handlersDict = handleKind switch
        {
            EKindOfHandler.Normal => _normalHandlers,
            EKindOfHandler.Delayed => _delayedHandlers,
            _ => throw new ArgumentOutOfRangeException(nameof(handleKind), handleKind, null)
        };
        handlersDict.AddOrUpdate(name, _ => [.. handlerTypes], (_, existingHandlers) =>
        {
            if (handlerTypes.Any(handlerType => !existingHandlers.Add(handlerType)))
            {
                throw new InvalidOperationException($"Handler type already registered for '{name}'");
            }
            return existingHandlers;
        });
    }
}
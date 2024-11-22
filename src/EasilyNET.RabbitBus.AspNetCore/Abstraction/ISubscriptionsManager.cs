using System.Reflection;
using EasilyNET.RabbitBus.AspNetCore.Enums;

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// Interface for managing subscriptions.
/// </summary>
internal interface ISubscriptionsManager
{
    /// <summary>
    /// Clears all subscription mappings.
    /// </summary>
    void ClearSubscriptions();

    /// <summary>
    /// Adds a subscription.
    /// </summary>
    /// <param name="eventType">The type of the event.</param>
    /// <param name="handleKind">The kind of event handler.</param>
    /// <param name="handlerTypes">The type information of the event handlers.</param>
    void AddSubscription(Type eventType, EKindOfHandler handleKind, IList<TypeInfo> handlerTypes);

    /// <summary>
    /// Gets the handlers for a specific event.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="handleKind">The kind of event handler.</param>
    /// <returns>An enumerable of handler types.</returns>
    IEnumerable<Type> GetHandlersForEvent(string name, EKindOfHandler handleKind);

    /// <summary>
    /// Checks if there are subscriptions for a specific event.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="handleKind">The kind of event handler.</param>
    /// <returns>True if there are subscriptions, otherwise false.</returns>
    bool HasSubscriptionsForEvent(string name, EKindOfHandler handleKind);
}
using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for retrieving custom attributes from event types.
/// </summary>
internal static class EventExtension
{
    // Caches for storing attribute dictionaries to improve performance
    private static readonly ConcurrentDictionary<Type, Lazy<IDictionary<string, object?>?>> HeaderAttributesCache = new();
    private static readonly ConcurrentDictionary<Type, Lazy<IDictionary<string, object?>?>> ExchangeArgAttributesCache = new();
    private static readonly ConcurrentDictionary<Type, Lazy<IDictionary<string, object?>?>> QueueArgAttributesCache = new();

    /// <summary>
    /// Gets the header attributes for the specified event.
    /// </summary>
    /// <param name="event">The event instance.</param>
    /// <returns>A dictionary of header attributes, or null if none are found.</returns>
    internal static IDictionary<string, object?>? GetHeaderAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        return HeaderAttributesCache.GetOrAdd(type, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<HeaderAttribute>()))).Value;
    }

    /// <summary>
    /// Gets the exchange argument attributes for the specified event.
    /// </summary>
    /// <param name="event">The event instance.</param>
    /// <returns>A dictionary of exchange argument attributes, or null if none are found.</returns>
    internal static IDictionary<string, object?>? GetExchangeArgAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        return ExchangeArgAttributesCache.GetOrAdd(type, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<ExchangeArgAttribute>()))).Value;
    }

    /// <summary>
    /// Gets the exchange argument attributes for the specified event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>A dictionary of exchange argument attributes, or null if none are found.</returns>
    internal static IDictionary<string, object?>? GetExchangeArgAttributes(this Type eventType)
    {
        return ExchangeArgAttributesCache.GetOrAdd(eventType, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<ExchangeArgAttribute>()))).Value;
    }

    /// <summary>
    /// Gets the queue argument attributes for the specified event.
    /// </summary>
    /// <param name="event">The event instance.</param>
    /// <returns>A dictionary of queue argument attributes, or null if none are found.</returns>
    internal static IDictionary<string, object?>? GetQueueArgAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        return QueueArgAttributesCache.GetOrAdd(type, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<QueueArgAttribute>()))).Value;
    }

    /// <summary>
    /// Gets the queue argument attributes for the specified event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>A dictionary of queue argument attributes, or null if none are found.</returns>
    internal static IDictionary<string, object?>? GetQueueArgAttributes(this Type eventType)
    {
        return QueueArgAttributesCache.GetOrAdd(eventType, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<QueueArgAttribute>()))).Value;
    }

    /// <summary>
    /// Converts a collection of RabbitDictionaryAttribute instances to a dictionary.
    /// </summary>
    /// <param name="rda_s">The collection of RabbitDictionaryAttribute instances.</param>
    /// <returns>A dictionary with the attribute keys and values.</returns>
    private static Dictionary<string, object?> RabbitDictionariesByDic(IEnumerable<RabbitDictionaryAttribute> rda_s)
    {
        return rda_s.ToDictionary(k => k.Key, v => v.Value);
    }
}
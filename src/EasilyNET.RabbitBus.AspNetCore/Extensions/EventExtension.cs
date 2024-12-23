using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.AspNetCore.Extensions;

/// <summary>
///     <para xml:lang="en">Provides extension methods for retrieving custom attributes from event types</para>
///     <para xml:lang="zh">提供用于从事件类型中检索自定义属性的扩展方法</para>
/// </summary>
internal static class EventExtension
{
    // Caches for storing attribute dictionaries to improve performance
    // 用于存储属性字典的缓存以提高性能
    private static readonly ConcurrentDictionary<Type, Lazy<IDictionary<string, object?>?>> HeaderAttributesCache = new();
    private static readonly ConcurrentDictionary<Type, Lazy<IDictionary<string, object?>?>> ExchangeArgAttributesCache = new();
    private static readonly ConcurrentDictionary<Type, Lazy<IDictionary<string, object?>?>> QueueArgAttributesCache = new();

    /// <summary>
    ///     <para xml:lang="en">Gets the header attributes for the specified event</para>
    ///     <para xml:lang="zh">获取指定事件的头属性</para>
    /// </summary>
    /// <param name="event">
    ///     <para xml:lang="en">The event instance</para>
    ///     <para xml:lang="zh">事件实例</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A dictionary of header attributes, or null if none are found</para>
    ///     <para xml:lang="zh">头属性的字典，如果没有找到则返回 null</para>
    /// </returns>
    internal static IDictionary<string, object?>? GetHeaderAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        return HeaderAttributesCache.GetOrAdd(type, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<HeaderAttribute>()))).Value;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the exchange argument attributes for the specified event</para>
    ///     <para xml:lang="zh">获取指定事件的交换参数属性</para>
    /// </summary>
    /// <param name="event">
    ///     <para xml:lang="en">The event instance</para>
    ///     <para xml:lang="zh">事件实例</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A dictionary of exchange argument attributes, or null if none are found</para>
    ///     <para xml:lang="zh">交换参数属性的字典，如果没有找到则返回 null</para>
    /// </returns>
    internal static IDictionary<string, object?>? GetExchangeArgAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        return ExchangeArgAttributesCache.GetOrAdd(type, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<ExchangeArgAttribute>()))).Value;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the exchange argument attributes for the specified event type</para>
    ///     <para xml:lang="zh">获取指定事件类型的交换参数属性</para>
    /// </summary>
    /// <param name="eventType">
    ///     <para xml:lang="en">The event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A dictionary of exchange argument attributes, or null if none are found</para>
    ///     <para xml:lang="zh">交换参数属性的字典，如果没有找到则返回 null</para>
    /// </returns>
    internal static IDictionary<string, object?>? GetExchangeArgAttributes(this Type eventType)
    {
        return ExchangeArgAttributesCache.GetOrAdd(eventType, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<ExchangeArgAttribute>()))).Value;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the queue argument attributes for the specified event</para>
    ///     <para xml:lang="zh">获取指定事件的队列参数属性</para>
    /// </summary>
    /// <param name="event">
    ///     <para xml:lang="en">The event instance</para>
    ///     <para xml:lang="zh">事件实例</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A dictionary of queue argument attributes, or null if none are found</para>
    ///     <para xml:lang="zh">队列参数属性的字典，如果没有找到则返回 null</para>
    /// </returns>
    internal static IDictionary<string, object?>? GetQueueArgAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        return QueueArgAttributesCache.GetOrAdd(type, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<QueueArgAttribute>()))).Value;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the queue argument attributes for the specified event type</para>
    ///     <para xml:lang="zh">获取指定事件类型的队列参数属性</para>
    /// </summary>
    /// <param name="eventType">
    ///     <para xml:lang="en">The event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A dictionary of queue argument attributes, or null if none are found</para>
    ///     <para xml:lang="zh">队列参数属性的字典，如果没有找到则返回 null</para>
    /// </returns>
    internal static IDictionary<string, object?>? GetQueueArgAttributes(this Type eventType)
    {
        return QueueArgAttributesCache.GetOrAdd(eventType, t =>
            new(() => RabbitDictionariesByDic(t.GetCustomAttributes<QueueArgAttribute>()))).Value;
    }

    /// <summary>
    ///     <para xml:lang="en">Converts a collection of RabbitDictionaryAttribute instances to a dictionary</para>
    ///     <para xml:lang="zh">将一组 RabbitDictionaryAttribute 实例转换为字典</para>
    /// </summary>
    /// <param name="rda_s">
    ///     <para xml:lang="en">The collection of RabbitDictionaryAttribute instances</para>
    ///     <para xml:lang="zh">RabbitDictionaryAttribute 实例的集合</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A dictionary with the attribute keys and values</para>
    ///     <para xml:lang="zh">包含属性键和值的字典</para>
    /// </returns>
    private static Dictionary<string, object?> RabbitDictionariesByDic(IEnumerable<RabbitDictionaryAttribute> rda_s)
    {
        return rda_s.ToDictionary(k => k.Key, v => v.Value);
    }
}
using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.AspNetCore.Extensions;

internal static class EventExtension
{
    private static readonly ConcurrentDictionary<Type, IDictionary<string, object?>?> HeaderAttributesCache = new();
    private static readonly ConcurrentDictionary<Type, IDictionary<string, object?>?> ExchangeArgAttributesCache = new();
    private static readonly ConcurrentDictionary<Type, IDictionary<string, object?>?> QueueArgAttributesCache = new();

    /// <summary>
    /// 获取Header的属性
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetHeaderAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        return HeaderAttributesCache.GetOrAdd(type, t => RabbitDictionariesByDic(t.GetCustomAttributes<HeaderAttribute>()));
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetExchangeArgAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        return ExchangeArgAttributesCache.GetOrAdd(type, t => RabbitDictionariesByDic(t.GetCustomAttributes<ExchangeArgAttribute>()));
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetExchangeArgAttributes(this Type eventType)
    {
        return ExchangeArgAttributesCache.GetOrAdd(eventType, t => RabbitDictionariesByDic(t.GetCustomAttributes<ExchangeArgAttribute>()));
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetQueueArgAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        return QueueArgAttributesCache.GetOrAdd(type, t => RabbitDictionariesByDic(t.GetCustomAttributes<QueueArgAttribute>()));
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetQueueArgAttributes(this Type eventType)
    {
        return QueueArgAttributesCache.GetOrAdd(eventType, t => RabbitDictionariesByDic(t.GetCustomAttributes<QueueArgAttribute>()));
    }

    /// <summary>
    /// 将特性内容转换成字典
    /// </summary>
    /// <param name="rda_s"></param>
    /// <returns></returns>
    private static Dictionary<string, object?>? RabbitDictionariesByDic(this IEnumerable<RabbitDictionaryAttribute>? rda_s)
    {
        return rda_s?.ToDictionary(k => k.Key, v => v.Value);
    }
}
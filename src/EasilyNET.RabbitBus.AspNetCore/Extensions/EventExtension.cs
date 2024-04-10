using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;
using System.Reflection;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.AspNetCore.Extensions;

internal static class EventExtension
{
    /// <summary>
    /// 获取Header的属性
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetHeaderAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        var rabbitHeaderAttributes = type.GetCustomAttributes<HeaderAttribute>();
        return RabbitDictionariesByDic(rabbitHeaderAttributes);
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetExchangeArgAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        var exchangeArgs = type.GetCustomAttributes<ExchangeArgAttribute>();
        return RabbitDictionariesByDic(exchangeArgs);
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetExchangeArgAttributes(this Type eventType)
    {
        var exchangeArgs = eventType.GetCustomAttributes<ExchangeArgAttribute>();
        return RabbitDictionariesByDic(exchangeArgs);
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetQueueArgAttributes(this IEvent @event)
    {
        var type = @event.GetType();
        var queueArgs = type.GetCustomAttributes<QueueArgAttribute>();
        return RabbitDictionariesByDic(queueArgs);
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns></returns>
    internal static IDictionary<string, object?>? GetQueueArgAttributes(this Type eventType)
    {
        var queueArgs = eventType.GetCustomAttributes<QueueArgAttribute>();
        return RabbitDictionariesByDic(queueArgs);
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
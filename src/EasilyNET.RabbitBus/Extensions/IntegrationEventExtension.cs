using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Core.Attributes;
using System.Reflection;

namespace EasilyNET.RabbitBus.Extensions;

internal static class IntegrationEventExtension
{
    /// <summary>
    /// 获取Header的属性
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    internal static IDictionary<string, object> GetHeaderAttributes(this IIntegrationEvent @event)
    {
        var type = @event.GetType();
        var rabbitHeaderAttributes = type.GetCustomAttributes<RabbitHeaderAttribute>();
        return RabbitDictionariesByDic(rabbitHeaderAttributes);
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    internal static IDictionary<string, object> GetExchangeArgAttributes(this IIntegrationEvent @event)
    {
        var type = @event.GetType();
        var exchangeArgs = type.GetCustomAttributes<RabbitExchangeArgAttribute>();
        return RabbitDictionariesByDic(exchangeArgs);
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns></returns>
    internal static IDictionary<string, object> GetExchangeArgAttributes(this Type eventType)
    {
        var exchangeArgs = eventType.GetCustomAttributes<RabbitExchangeArgAttribute>();
        return RabbitDictionariesByDic(exchangeArgs);
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    internal static IDictionary<string, object> GetQueueArgAttributes(this IIntegrationEvent @event)
    {
        var type = @event.GetType();
        var queueArgs = type.GetCustomAttributes<RabbitQueueArgAttribute>();
        return RabbitDictionariesByDic(queueArgs);
    }

    /// <summary>
    /// 获取交换机的Arguments
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns></returns>
    internal static IDictionary<string, object> GetQueueArgAttributes(this Type eventType)
    {
        var queueArgs = eventType.GetCustomAttributes<RabbitQueueArgAttribute>();
        return RabbitDictionariesByDic(queueArgs);
    }

    /// <summary>
    /// 将特性内容转换成字典
    /// </summary>
    /// <param name="rabbitDictionaryAttributes"></param>
    /// <returns></returns>
    private static Dictionary<string, object> RabbitDictionariesByDic(this IEnumerable<RabbitDictionaryAttribute> rabbitDictionaryAttributes)
    {
        return rabbitDictionaryAttributes.ToDictionary(rabbitDictionaryAttribute => rabbitDictionaryAttribute.Key,
            rabbitDictionaryAttribute => rabbitDictionaryAttribute.Value);
    }
}
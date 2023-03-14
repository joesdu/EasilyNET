using EasilyNET.RabbitBus.Abstractions;
using EasilyNET.RabbitBus.Attributes;
using System.Reflection;

namespace EasilyNET.RabbitBus.Extensions;

internal static class IntegrationEventExtension
{
    internal static IDictionary<string, object> GetHeaderAttributes(this IIntegrationEvent @event)
    {
        var type = @event.GetType();
        var rabbitHeaderAttributes = type.GetCustomAttributes<RabbitHeaderAttribute>();
        return RabbitDictionariesByDic(rabbitHeaderAttributes);
    }

    internal static IDictionary<string, object> GetArgAttributes(this IIntegrationEvent @event)
    {
        var type = @event.GetType();
        var rabbitArgAttributes = type.GetCustomAttributes<RabbitArgAttribute>();
        return RabbitDictionariesByDic(rabbitArgAttributes);
    }

    internal static IDictionary<string, object> GetArgAttributes(this Type eventType)
    {
        var rabbitArgAttributes = eventType.GetCustomAttributes<RabbitArgAttribute>();
        return RabbitDictionariesByDic(rabbitArgAttributes);
    }

    private static Dictionary<string, object> RabbitDictionariesByDic(this IEnumerable<RabbitDictionaryAttribute> rabbitDictionaryAttributes)
    {
        return rabbitDictionaryAttributes.ToDictionary(rabbitDictionaryAttribute => rabbitDictionaryAttribute.Key,
            rabbitDictionaryAttribute => rabbitDictionaryAttribute.Value);
    }
}
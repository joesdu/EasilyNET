using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Enums;
using EasilyNET.RabbitBus.AspNetCore.Serializer;
using Microsoft.Extensions.Options;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// Factory for creating bus serializers based on configuration.
/// </summary>
internal sealed class BusSerializerFactory(IOptionsMonitor<RabbitConfig> options) : IBusSerializerFactory
{
    /// <summary>
    /// Creates a bus serializer based on the current configuration.
    /// </summary>
    /// <returns>An instance of <see cref="IBusSerializer" />.</returns>
    public IBusSerializer CreateSerializer()
    {
        var config = options.Get(Constant.OptionName);
        return config.Serializer switch
        {
            ESerializer.TextJson    => new TextJsonSerializer(),
            ESerializer.MessagePack => new MsgPackSerializer(),
            _                       => throw new ArgumentException("Invalid serializer type")
        };
    }
}
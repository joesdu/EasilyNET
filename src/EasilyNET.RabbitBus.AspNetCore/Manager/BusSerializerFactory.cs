using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Enums;
using EasilyNET.RabbitBus.AspNetCore.Serializer;
using Microsoft.Extensions.Options;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// SerializerFactory工厂
/// </summary>
internal class BusSerializerFactory(IOptionsMonitor<RabbitConfig> options) : IBusSerializerFactory
{
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
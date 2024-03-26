using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using MessagePack;
using MessagePack.Resolvers;

namespace EasilyNET.RabbitBus.AspNetCore.Serializer;

/// <summary>
/// MyMessagePackSerializer
/// </summary>
internal sealed class MsgPackSerializer : IBusSerializer
{
    private static readonly MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

    /// <inheritdoc />
    public byte[] Serialize(object? obj, Type type) => MessagePackSerializer.Serialize(type, obj, options);

    /// <inheritdoc />
    public object? Deserialize(byte[] data, Type type) => MessagePackSerializer.Deserialize(type, data, options);
}
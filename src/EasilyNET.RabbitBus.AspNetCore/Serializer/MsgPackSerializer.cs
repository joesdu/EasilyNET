using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using MessagePack;
using MessagePack.Resolvers;

namespace EasilyNET.RabbitBus.AspNetCore.Serializer;

/// <summary>
/// MessagePackSerializer
/// </summary>
internal sealed class MsgPackSerializer : IBusSerializer
{
    private static readonly MessagePackSerializerOptions standardOptions =
        MessagePackSerializerOptions.Standard
                                    .WithResolver(CompositeResolver.Create(
                                        NativeDateTimeResolver.Instance,     // 使用本地日期时间解析器
                                        ContractlessStandardResolver.Instance))            // 使用无合约标准解析器
                                    .WithSecurity(MessagePackSecurity.UntrustedData);      // 设置安全选项以处理不受信任的数据

    /// <summary>
    /// 使用 LZ4 算法对整个数组进行压缩.这种方式适用于需要对大量数据进行压缩的场景,压缩效率较高
    /// </summary>
    private static readonly MessagePackSerializerOptions lz4BlockArrayOptions =
        standardOptions.WithCompression(MessagePackCompression.Lz4BlockArray);

    /// <summary>
    /// 使用 LZ4 算法对每个数据块进行压缩.这种方式适用于需要对单个数据块进行压缩的场景,压缩速度较快
    /// </summary>
    private static readonly MessagePackSerializerOptions lz4BlockOptions =
        standardOptions.WithCompression(MessagePackCompression.Lz4Block);

    /// <inheritdoc />
    public byte[] Serialize(object? obj, Type type)
    {
        var data = MessagePackSerializer.Serialize(type, obj, standardOptions);
        var options = data.Length > 8192 ? lz4BlockArrayOptions : lz4BlockOptions;
        return MessagePackSerializer.Serialize(type, obj, options);
    }

    /// <inheritdoc />
    public object? Deserialize(byte[] data, Type type)
    {
        var options = data.Length > 8192 ? lz4BlockArrayOptions : lz4BlockOptions;
        return MessagePackSerializer.Deserialize(type, data, options);
    }
}
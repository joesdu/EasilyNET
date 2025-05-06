namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
///     <para xml:lang="en">Interface for a serializer used in the bus</para>
///     <para xml:lang="zh">用于消息总线的序列化器接口</para>
/// </summary>
public interface IBusSerializer
{
    /// <summary>
    ///     <para xml:lang="en">Serializes an object to a byte array</para>
    ///     <para xml:lang="zh">将对象序列化为字节数组</para>
    /// </summary>
    /// <param name="obj">
    ///     <para xml:lang="en">The object to serialize</para>
    ///     <para xml:lang="zh">要序列化的对象</para>
    /// </param>
    /// <param name="type">
    ///     <para xml:lang="en">The type of the object</para>
    ///     <para xml:lang="zh">对象的类型</para>
    /// </param>
    byte[] Serialize(object? obj, Type type);

    /// <summary>
    ///     <para xml:lang="en">Deserializes a byte array to an object</para>
    ///     <para xml:lang="zh">将字节数组反序列化为对象</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The byte array to deserialize</para>
    ///     <para xml:lang="zh">要反序列化的字节数组</para>
    /// </param>
    /// <param name="type">
    ///     <para xml:lang="en">The target type of the deserialized object</para>
    ///     <para xml:lang="zh">反序列化对象的目标类型</para>
    /// </param>
    object? Deserialize(byte[] data, Type type);
}
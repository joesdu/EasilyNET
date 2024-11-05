namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// 序列化器接口
/// </summary>
internal interface IBusSerializer
{
    /// <summary>
    /// 序列化对象
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="type">对象类型</param>
    /// <returns></returns>
    byte[] Serialize(object? obj, Type type);

    /// <summary>
    /// 反序列化对象
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type">目标类型</param>
    /// <returns></returns>
    object? Deserialize(byte[] data, Type type);
}
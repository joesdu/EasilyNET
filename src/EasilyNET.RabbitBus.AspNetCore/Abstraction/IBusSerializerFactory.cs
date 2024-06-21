namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// SerializerFactory工厂
/// </summary>
internal interface IBusSerializerFactory
{
    /// <summary>
    /// 创建序列化器
    /// </summary>
    /// <returns></returns>
    IBusSerializer CreateSerializer();
}
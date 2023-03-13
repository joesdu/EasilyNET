// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Attributes;

/// <summary>
/// 添加RabbitMQ,Headers参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RabbitHeaderAttribute : RabbitDictionaryAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public RabbitHeaderAttribute(string key, object value) : base(key, value) { }
}
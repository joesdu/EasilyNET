// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Attributes;

/// <summary>
/// RabbitMQ参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RabbitArgAttribute : RabbitDictionaryAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="key">RabbitMQ参数key</param>
    /// <param name="value">值</param>
    public RabbitArgAttribute(string key, object value) : base(key, value) { }
}
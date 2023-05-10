// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// RabbitMQ交换机参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RabbitExchangeArgAttribute : RabbitDictionaryAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="key">RabbitMQ参数key</param>
    /// <param name="value">值</param>
    public RabbitExchangeArgAttribute(string key, object value) : base(key, value) { }
}
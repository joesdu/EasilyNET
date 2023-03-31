namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// 作用于队列的Arguments
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
// ReSharper disable once ClassNeverInstantiated.Global
public class RabbitQueueArgAttribute : RabbitDictionaryAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="key">RabbitMQ参数key</param>
    /// <param name="value">值</param>
    public RabbitQueueArgAttribute(string key, object value) : base(key, value) { }
}
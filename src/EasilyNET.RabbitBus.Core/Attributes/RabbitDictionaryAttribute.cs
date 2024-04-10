namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// Rabbit字典
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RabbitDictionaryAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    protected RabbitDictionaryAttribute(string key, object? value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// 键
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 值
    /// </summary>
    public object? Value { get; }
}
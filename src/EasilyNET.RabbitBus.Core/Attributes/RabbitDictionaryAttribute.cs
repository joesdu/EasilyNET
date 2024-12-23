namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
///     <para xml:lang="en">Rabbit dictionary</para>
///     <para xml:lang="zh">Rabbit字典</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RabbitDictionaryAttribute : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The key</para>
    ///     <para xml:lang="zh">键</para>
    /// </param>
    /// <param name="value">
    ///     <para xml:lang="en">The value</para>
    ///     <para xml:lang="zh">值</para>
    /// </param>
    protected RabbitDictionaryAttribute(string key, object? value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    ///     <para xml:lang="en">The key</para>
    ///     <para xml:lang="zh">键</para>
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///     <para xml:lang="en">The value</para>
    ///     <para xml:lang="zh">值</para>
    /// </summary>
    public object? Value { get; }
}
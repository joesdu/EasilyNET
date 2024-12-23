namespace EasilyNET.Core.Attributes;

/// <summary>
///     <para xml:lang="en">Base attribute class</para>
///     <para xml:lang="zh">基础特性</para>
/// </summary>
public abstract class AttributeBase : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">Get the description</para>
    ///     <para xml:lang="zh">获取描述</para>
    /// </summary>
    /// <returns></returns>
    public abstract string Description();
}
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Core.Attributes;

/// <summary>
///     <para xml:lang="en">This attribute can be used to group controllers in Swagger documentation.</para>
///     <para xml:lang="zh">被此特性标记的控制器可在Swagger文档分组中发挥作用.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiGroupAttribute(string title, string des = "") : AttributeBase
{
    /// <summary>
    ///     <para xml:lang="en">Title</para>
    ///     <para xml:lang="zh">标题</para>
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    ///     <para xml:lang="en">Description</para>
    ///     <para xml:lang="zh">描述</para>
    /// </summary>
    public string Des { get; } = des;

    /// <summary>
    ///     <para xml:lang="en">Get the description of the attribute</para>
    ///     <para xml:lang="zh">获取特性的描述信息</para>
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public override string Description() => "被此特性标记的控制器可在Swagger文档分组中发挥作用. This attribute can be used to group controllers in Swagger documentation.";
}
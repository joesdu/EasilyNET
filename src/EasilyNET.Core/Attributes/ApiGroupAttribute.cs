// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Core.Attributes;

/// <summary>
/// 被此特性标记的控制器可在Swagger文档分组中发挥作用.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiGroupAttribute(string title, string des = "") : AttributeBase
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// 描述
    /// </summary>
    public string Des { get; } = des;

    /// <summary>
    /// 获取特性的描述信息
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override string Description() => "被此特性标记的控制器可在Swagger文档分组中发挥作用";
}
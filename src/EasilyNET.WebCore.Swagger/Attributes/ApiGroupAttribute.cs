// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.WebCore.Swagger.Attributes;

/// <summary>
/// 被此特性标记的控制器可在Swagger文档分组中发挥作用.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiGroupAttribute(string title, string des = "", bool defaultDes = false) : Attribute
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
    /// 设置当前分组描述为该分组的默认描述
    /// </summary>
    public bool DefaultDes { get; } = defaultDes;
}
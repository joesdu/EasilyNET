// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.WebCore.Attributes;

/// <summary>
/// 被此特性标记的控制器可在Swagger文档分组中发挥作用.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
// ReSharper disable once UnusedMember.Global
// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ApiGroupAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="title"></param>
    /// <param name="version"></param>
    /// <param name="description"></param>
    public ApiGroupAttribute(string title, string version, string description = "")
    {
        Title = title;
        Version = version;
        Description = description;
        Name = $"{title}-{version}";
    }

    /// <summary>
    /// Doc名称,$"{Title}-{Version}"格式
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; }
}
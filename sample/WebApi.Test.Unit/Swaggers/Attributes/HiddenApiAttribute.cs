using EasilyNET.Core.Attributes;

namespace WebApi.Test.Unit.Swaggers.Attributes;

/// <summary>
///     <para xml:lang="en">This attribute can be used to hide actions or controllers in Swagger documentation.</para>
///     <para xml:lang="zh">被此特性标记的Action或者控制器可在Swagger文档中隐藏.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HiddenApiAttribute : AttributeBase
{
    /// <summary>
    ///     <para xml:lang="en">Get the description of the attribute</para>
    ///     <para xml:lang="zh">获取特性的描述信息</para>
    /// </summary>
    public override string Description() => "被此特性标记的Action或者控制器可在Swagger文档中隐藏. This attribute can be used to hide actions or controllers in Swagger documentation.";
}
namespace EasilyNET.Core.Attributes;

/// <summary>
/// 被此特性标记的Action或者控制器可在Swagger文档中隐藏.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HiddenApiAttribute : AttributeBase
{
    /// <summary>
    /// 获取特性的描述信息
    /// </summary>
    /// <returns></returns>
    public override string Description() => "被此特性标记的Action或者控制器可在Swagger文档中隐藏";
}
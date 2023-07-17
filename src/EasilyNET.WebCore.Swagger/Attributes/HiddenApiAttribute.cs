namespace EasilyNET.WebCore.Swagger.Attributes;

/// <summary>
/// 被此特性标记的Action或者控制器可在Swagger文档中隐藏.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HiddenApiAttribute : Attribute;
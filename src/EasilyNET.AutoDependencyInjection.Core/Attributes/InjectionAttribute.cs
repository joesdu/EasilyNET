namespace EasilyNET.AutoDependencyInjection.Core.Attributes;

/// <summary>
/// 属性注入,被该特性标记的属性或者字段可实现自动注入
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class InjectionAttribute : Attribute;
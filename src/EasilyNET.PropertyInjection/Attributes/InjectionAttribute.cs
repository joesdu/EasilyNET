// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.PropertyInjection.Attributes;

/// <summary>
/// 实现属性注入
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class InjectionAttribute : Attribute;
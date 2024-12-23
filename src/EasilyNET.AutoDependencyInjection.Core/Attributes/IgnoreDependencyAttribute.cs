// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.AutoDependencyInjection.Core.Attributes;

/// <summary>
///     <para xml:lang="en">Configure this attribute to ignore automatic dependency injection mapping</para>
///     <para xml:lang="zh">配置此特性将忽略依赖注入自动映射</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class IgnoreDependencyAttribute : Attribute;
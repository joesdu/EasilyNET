using Microsoft.Extensions.DependencyInjection;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.AutoDependencyInjection.Core.Attributes;

/// <summary>
///     <para xml:lang="en">Automatically inject services with this attribute</para>
///     <para xml:lang="zh">配置此特性将自动进行注入</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DependencyInjectionAttribute(ServiceLifetime lifetime) : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">Service lifetime</para>
    ///     <para xml:lang="zh">服务生命周期</para>
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets whether to register the type itself. By default, types without interfaces will register themselves. When this
    ///     property is true, the type will also register itself.
    ///     </para>
    ///     <para xml:lang="zh">获取或设置是否注册自身类型，默认没有接口的类型会注册自身，当此属性值为 true 时，也会注册自身</para>
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool AddSelf { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Only register the type itself, without registering interfaces</para>
    ///     <para xml:lang="zh">仅注册自身类型，而不注册接口</para>
    /// </summary>
    public bool SelfOnly { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Set the service key, compatible with KeyedService</para>
    ///     <para xml:lang="zh">设置服务的键，适配 KeyedService</para>
    /// </summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Register as a specific type. Explicitly setting this value can reduce reflection and improve performance. If the
    ///     implementation class is not a derived class of the registered type, registration will be skipped.
    ///     </para>
    ///     <para xml:lang="zh">注册为什么类型，明确该值会减少反射提升性能，若实现类不是注册类型的派生类，则会跳过注册</para>
    /// </summary>
    public Type? AsType { get; set; }
}
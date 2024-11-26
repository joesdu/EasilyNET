using Microsoft.Extensions.DependencyInjection;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.AutoDependencyInjection.Core.Attributes;

/// <summary>
/// 配置此特性将自动进行注入
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DependencyInjectionAttribute(ServiceLifetime lifetime) : Attribute
{
    /// <summary>
    /// 服务生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;

    /// <summary>
    /// 获取或设置 是否注册自身类型，默认没有接口的类型会注册自身，当此属性值为true时，也会注册自身
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool AddSelf { get; set; }

    /// <summary>
    /// 仅注册自身类型,而不注册接口
    /// </summary>
    public bool SelfOnly { get; set; }

    /// <summary>
    /// 设置服务的键,适配KeyedService
    /// </summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary>
    /// 注册为什么类型,明确该值会减少反射提升性能,若实现类,不是注册类型的派生类,则会跳过注册
    /// </summary>
    public Type? AsType { get; set; }
}
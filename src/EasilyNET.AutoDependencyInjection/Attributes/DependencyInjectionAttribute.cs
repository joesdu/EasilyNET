using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.AutoDependencyInjection.Attributes;

/// <summary>
/// 配置此特性将自动进行注入
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DependencyInjectionAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="lifetime">注入类型(Scoped\Singleton\Transient)</param>
    public DependencyInjectionAttribute(ServiceLifetime lifetime) => Lifetime = lifetime;
    /// <summary>
    /// 服务生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; }
    /// <summary>
    /// 获取或设置 是否注册自身类型，默认没有接口的类型会注册自身，当此属性值为true时，也会注册自身
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool AddSelf { get; set; }
}
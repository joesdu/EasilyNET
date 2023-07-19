using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.AutoDependencyInjection.Core.Abstracts;

/// <summary>
/// 属性注入提供者
/// </summary>
public interface IPropertyInjectionServiceProvider : IServiceProvider, ISupportRequiredService
{
    /// <summary>
    /// 判断注入属性
    /// </summary>
    /// <param name="instance"></param>
    void IsInjectProperties(object instance);
}
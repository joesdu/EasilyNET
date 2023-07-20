using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.AutoDependencyInjection.Abstracts;

/// <summary>
/// 属性注入提供者接口
/// </summary>
internal interface IPropertyInjectionServiceProvider : IServiceProvider, ISupportRequiredService
{
    /// <summary>
    /// 判断是否需要属性注入
    /// </summary>
    /// <param name="instance">实例</param>
    void IsInjectProperties(object instance);
}
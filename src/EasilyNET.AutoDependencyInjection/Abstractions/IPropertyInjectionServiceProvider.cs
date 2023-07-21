using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.AutoDependencyInjection.Abstracts;

/// <summary>
/// 属性注入提供者接口
/// </summary>
internal interface IPropertyInjectionServiceProvider : IServiceProvider, ISupportRequiredService
{

}
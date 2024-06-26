using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.AutoDependencyInjection.Core.Abstractions;

/// <summary>
/// 实现此接口的类型将自动注册为<see cref="ServiceLifetime.Singleton" /> 模式
/// </summary>
[IgnoreDependency]
public interface ISingletonDependency
{
    /// <summary>
    /// 是否添加自身
    /// </summary>
    static bool? AddSelf { get; set; } = false;
}
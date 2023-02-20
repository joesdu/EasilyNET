using EasilyNET.DependencyInjection.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.DependencyInjection.Abstractions;
/// <summary>
/// 实现此接口的类型将自动注册为<see cref="ServiceLifetime.Scoped"/>模式
/// </summary>
[IgnoreDependency]
public interface IScopedDependency { }
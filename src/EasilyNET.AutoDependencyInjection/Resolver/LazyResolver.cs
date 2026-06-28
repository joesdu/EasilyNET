using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Resolver;

/// <summary>
///     <para xml:lang="en">
///     Default implementation that provides Autofac-style implicit <see cref="Lazy{T}" /> support on top of
///     Microsoft.Extensions.DependencyInjection. When injected, resolution of <typeparamref name="T" /> is deferred
///     until <see cref="Lazy{T}.Value" /> is first accessed, and is performed against the same
///     <see cref="IServiceProvider" /> (and thus the same scope) as the consuming component.
///     </para>
///     <para xml:lang="zh">
///     在 Microsoft.Extensions.DependencyInjection 之上提供 Autofac 风格的隐式 <see cref="Lazy{T}" /> 支持的默认实现。
///     被注入时，会延迟到首次访问 <see cref="Lazy{T}.Value" /> 才解析 <typeparamref name="T" />，
///     且使用与消费方组件相同的 <see cref="IServiceProvider" />（即相同作用域）进行解析。
///     </para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The service type to resolve lazily</para>
///     <para xml:lang="zh">要延迟解析的服务类型</para>
/// </typeparam>
/// <param name="provider">
///     <para xml:lang="en">The service provider used to resolve <typeparamref name="T" /> on first access</para>
///     <para xml:lang="zh">首次访问时用于解析 <typeparamref name="T" /> 的服务提供者</para>
/// </param>
internal sealed class LazyResolver<T>(IServiceProvider provider) : Lazy<T>(provider.GetRequiredService<T>) where T : notnull;

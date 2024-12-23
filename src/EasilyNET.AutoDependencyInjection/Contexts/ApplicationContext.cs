using EasilyNET.AutoDependencyInjection.Abstractions;

namespace EasilyNET.AutoDependencyInjection.Contexts;

/// <inheritdoc />
public sealed class ApplicationContext : IServiceProviderAccessor
{
    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="serviceProvider">
    ///     <para xml:lang="en">Service provider</para>
    ///     <para xml:lang="zh">服务提供者</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Parameter {<code>nameof(serviceProvider)</code>} cannot be null</para>
    ///     <para xml:lang="zh">参数{<code>nameof(serviceProvider)</code>}不能为空</para>
    /// </exception>
    public ApplicationContext(IServiceProvider? serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    ///     <para xml:lang="zh">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; }
}
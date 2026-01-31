namespace EasilyNET.AutoDependencyInjection.Contexts;

/// <summary>
///     <para xml:lang="en">Application initialization context, provides access to the built ServiceProvider</para>
///     <para xml:lang="zh">应用初始化上下文，提供对已构建的 ServiceProvider 的访问</para>
/// </summary>
/// <param name="serviceProvider">
///     <para xml:lang="en">The built service provider</para>
///     <para xml:lang="zh">已构建的服务提供者</para>
/// </param>
public sealed class ApplicationContext(IServiceProvider serviceProvider)
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    ///     <para xml:lang="zh">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
}
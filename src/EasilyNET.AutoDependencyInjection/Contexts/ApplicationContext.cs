using EasilyNET.AutoDependencyInjection.Abstractions;

namespace EasilyNET.AutoDependencyInjection.Contexts;

/// <inheritdoc />
public sealed class ApplicationContext : IServiceProviderAccessor
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <exception cref="ArgumentNullException">参数{<code>nameof(serviceProvider)</code>}不能为空</exception>
    public ApplicationContext(IServiceProvider? serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// IServiceProvider
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; }
}
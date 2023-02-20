using EasilyNET.DependencyInjection.Abstractions;

namespace EasilyNET.DependencyInjection.Contexts;

/// <summary>
/// 自定义应用上下文
/// </summary>
public sealed class ApplicationContext : IServiceProviderAccessor
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <exception cref="ArgumentNullException">参数{<code>nameof(serviceProvider)</code>}不能为空</exception>
    public ApplicationContext(IServiceProvider? serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException($"参数“{nameof(serviceProvider)}”不能为空引用");
    }
    /// <summary>
    /// IServiceProvider
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; }
}
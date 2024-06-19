using EasilyNET.AutoDependencyInjection.Abstractions;

// ReSharper disable UnusedMethodReturnValue.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection" /> 扩展
/// </summary>
public static partial class ServiceCollectionExtension
{
    /// <summary>
    /// 试着添加对象适配器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    internal static void TryAddObjectAccessor<T>(this IServiceCollection services)
    {
        if (services.All(s => s.ServiceType != typeof(IObjectAccessor<T>)))
        {
            services.AddObjectAccessor<T>();
        }
    }
}
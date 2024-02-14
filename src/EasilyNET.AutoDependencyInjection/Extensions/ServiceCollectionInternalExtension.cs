using EasilyNET.AutoDependencyInjection;

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
    internal static ObjectAccessor<T> TryAddObjectAccessor<T>(this IServiceCollection services)
    {
        return services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>))
                   ? services.GetSingletonInstance<ObjectAccessor<T>>()
                   : services.AddObjectAccessor<T>();
    }
}

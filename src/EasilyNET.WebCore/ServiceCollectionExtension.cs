using EasilyNET.Core.Abstractions;
using EasilyNET.WebCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// 注册当前用户
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentWebUser>();
        return services;
    }
}

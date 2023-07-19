using Microsoft.Extensions.Hosting;

namespace  EasilyNET.PropertyInjection;

/// <summary>
/// 扩展
/// </summary>
public static class PropertyInjectionExtension
{

    /// <summary>
    /// 使用属性注入
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    public static IHostBuilder UseDefaultPropertyInjection(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseServiceProviderFactory(new PropertyInjectionServiceProviderFactory());
    }
}
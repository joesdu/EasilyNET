using Microsoft.Extensions.Hosting;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.PropertyInjection;

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
    public static void UseDefaultPropertyInjection(this IHostBuilder hostBuilder) => hostBuilder.UseServiceProviderFactory(new PropertyInjectionServiceProviderFactory());
}
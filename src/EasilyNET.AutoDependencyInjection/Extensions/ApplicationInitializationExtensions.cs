using EasilyNET.DependencyInjection.Abstractions;
using EasilyNET.DependencyInjection.Contexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.DependencyInjection.Extensions;
/// <summary>
/// ApplicationInitialization扩展
/// </summary>
public static class ApplicationInitializationExtensions
{
    /// <summary>
    /// 获取应用程序构建器
    /// </summary>
    /// <param name="applicationContext"></param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IApplicationBuilder GetApplicationBuilder(this ApplicationContext applicationContext) => applicationContext.ServiceProvider.GetRequiredService<IObjectAccessor<IApplicationBuilder>>().Value!;
}
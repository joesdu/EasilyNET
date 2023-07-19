using EasilyNET.PropertyInjection.AspNetCore;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 扩展
/// </summary>
public static class MvcBuilderExtension
{
    
    /// <summary>
    /// 添加属性服务
    /// </summary>
    /// <param name="mvcBuilder"></param>
    /// <returns></returns>
    public static IMvcBuilder AddPropertyInjectionAsServices(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.AddControllersAsServices();
        mvcBuilder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, PropertyInjectionControllerActivator>());
        return mvcBuilder;
    }
}


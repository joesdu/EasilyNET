using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入控制器激活器
/// </summary>
public class PropertyInjectionControllerActivator : IControllerActivator
{
    /// <summary>
    /// 创建
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public object Create(ControllerContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
        var serviceProvider = context.HttpContext.RequestServices;
        if (serviceProvider is not PropertyInjectionServiceProvider)
        {
            serviceProvider = new PropertyInjectionServiceProvider(context.HttpContext.RequestServices);
        }
        var controller = serviceProvider.GetRequiredService(controllerType);
        return controller;
    }

    /// <summary>
    /// 替换
    /// </summary>
    /// <param name="context"></param>
    /// <param name="controller"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Release(ControllerContext context, object controller)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(controller, nameof(controller));
        var disposable = controller as IDisposable;
        disposable?.Dispose();
    }
}
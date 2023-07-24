using EasilyNET.Core.Misc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入控制器激活器
/// </summary>
internal sealed class PropertyInjectionControllerActivator : IControllerActivator
{
    /// <summary>
    /// 创建
    /// </summary>
    /// <param name="context">控制器上下器</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public object Create(ControllerContext context)
    {
        context.NotNull(nameof(context));
        // ArgumentNullException.ThrowIfNull(context, nameof(context));  ??为什么使用这个？？
        var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
        var serviceProvider = context.HttpContext.RequestServices;
        if (serviceProvider is not PropertyInjectionServiceProvider) //判断是否属性注入服务提供者
        {
            serviceProvider = new PropertyInjectionServiceProvider(context.HttpContext.RequestServices);
        }
        var controller = serviceProvider.GetRequiredService(controllerType);
        return controller;
    }

    /// <summary>
    /// 替换
    /// </summary>
    /// <param name="context">控制器上下文</param>
    /// <param name="controller">控制器</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Release(ControllerContext context, object controller)
    {
        // ArgumentNullException.ThrowIfNull(context, nameof(context));
        // ArgumentNullException.ThrowIfNull(controller, nameof(controller));
        context.NotNull(nameof(context));
        controller.NotNull(nameof(controller));
        var disposable = controller as IDisposable;
        disposable?.Dispose();
    }
}
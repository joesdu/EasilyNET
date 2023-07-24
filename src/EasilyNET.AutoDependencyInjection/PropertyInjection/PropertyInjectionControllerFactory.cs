using EasilyNET.AutoDependencyInjection.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入控制器工厂，创建控制器
/// </summary>
/// <remarks>
/// </remarks>
/// <param name="propertyInjector">属性注入注射器接口</param>
/// <param name="controllerActivator">控制器激活顺</param>
internal sealed class PropertyInjectionControllerFactory(IPropertyInjector propertyInjector, IControllerActivator controllerActivator) : IControllerFactory
{
    /// <summary>
    /// 创建控制器
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public object CreateController(ControllerContext context) => propertyInjector.InjectProperties(controllerActivator.Create(context));

    /// <summary>
    /// 替换控制器
    /// </summary>
    /// <param name="context"></param>
    /// <param name="controller"></param>
    public void ReleaseController(ControllerContext context, object controller) => controllerActivator.Release(context, controller);
}
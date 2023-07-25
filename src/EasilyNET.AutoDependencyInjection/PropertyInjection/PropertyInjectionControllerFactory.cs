using EasilyNET.AutoDependencyInjection.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <inheritdoc />
internal sealed class PropertyInjectionControllerFactory(IPropertyInjector propertyInjector, IControllerActivator controllerActivator) : IControllerFactory
{
    /// <inheritdoc />
    public object CreateController(ControllerContext context) => propertyInjector.InjectProperties(controllerActivator.Create(context));

    /// <inheritdoc />
    public void ReleaseController(ControllerContext context, object controller) => controllerActivator.Release(context, controller);
}
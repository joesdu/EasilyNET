using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Abstracts;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入提供者
/// </summary>
internal sealed class PropertyInjectionServiceProvider : IPropertyInjectionServiceProvider
{
    private readonly IPropertyInjector _propertyInjector;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;

    /// <summary>
    /// </summary>
    public PropertyInjectionServiceProvider(IServiceCollection service)
    {
        _services = service ?? throw new ArgumentNullException(nameof(service));
        _services = service.AddSingleton<IPropertyInjectionServiceProvider>(this);
        InjectServices(service);
        _propertyInjector = new PropertyInjector(this);
        _serviceProvider = service.BuildServiceProvider();
    }

    /// <summary>
    /// 得到服务
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object? GetService(Type serviceType)
    {
        var instance = _serviceProvider.GetService(serviceType);
        return instance is null ? null : _propertyInjector.InjectProperties(instance);
    }

    /// <summary>
    /// 得到所需服务
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object GetRequiredService(Type serviceType)
    {
        var service = GetService(serviceType);
        ArgumentNullException.ThrowIfNull(service);
        return service;
    }

    private void InjectServices(IServiceCollection services)
    {
        services.Where(IsInjectable).ToList().ForEach(InjectDescriptor);
    }

    private void InjectDescriptor(ServiceDescriptor descriptor) => _services.Replace(new(descriptor.ServiceType, CreateFactory(descriptor), descriptor.Lifetime));

    private static bool IsInjectable(ServiceDescriptor descriptor) => InvokeMethod<Type>(descriptor, "GetImplementationType").HasAttribute<InjectionAttribute>();

    private static T InvokeMethod<T>(object instance, string methodName)
    {
        var obj_instance = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)?.Invoke(instance, null);
        ArgumentNullException.ThrowIfNull(obj_instance);
        return (T)obj_instance;
    }

    private Func<IServiceProvider, object> CreateFactory(ServiceDescriptor descriptor) => _ => _propertyInjector.InjectProperties(CreateInstance(descriptor));

    private object CreateInstance(ServiceDescriptor descriptor) =>
        descriptor.ImplementationInstance ??
        descriptor.ImplementationFactory?.Invoke(this) ??
        ActivatorUtilities.CreateInstance(this, descriptor.ImplementationType ?? throw new ArgumentNullException(nameof(descriptor.ImplementationType), "ImplementationType is null"));
}
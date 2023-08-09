using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Abstracts;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <inheritdoc />
internal sealed class PropertyInjectionServiceProvider : IPropertyInjectionServiceProvider
{
    private readonly IPropertyInjector _propertyInjector;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;

    public PropertyInjectionServiceProvider(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        services.AddSingleton<IPropertyInjectionServiceProvider>(this);
        _services = services;
        InjectServices(services);
        _serviceProvider = _services.BuildServiceProvider();
        _propertyInjector = new PropertyInjector(this);
    }

    private static BindingFlags BindingFlags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        var instance = GetAnyOriginalService(serviceType);
        return instance is null ? null : _propertyInjector.InjectProperties(instance);
    }

    //private Type GetAssignableService(Type type) => _services.First(s => s.ServiceType.IsAssignableTo(type)).ServiceType;

    /// <inheritdoc />
    public object GetRequiredService(Type serviceType)
    {
        var service = GetService(serviceType);
        ArgumentNullException.ThrowIfNull(service);
        return service;
    }

    private object? GetAnyOriginalService(Type type) => _serviceProvider.GetService(type);

    private void InjectServices(IServiceCollection services) => services.Where(IsInjectable).ToList().ForEach(InjectDescriptor);

    /// <summary>
    /// 注入Descriptor
    /// </summary>
    /// <param name="descriptor"></param>
    private void InjectDescriptor(ServiceDescriptor descriptor) => _services.Replace(new(descriptor.ServiceType, CreateFactory(descriptor), descriptor.Lifetime));

    /// <summary>
    /// 是否注入
    /// </summary>
    /// <param name="descriptor"></param>
    /// <returns></returns>
    private static bool IsInjectable(ServiceDescriptor descriptor)
    {
        var method = InvokeMethod<Type>(descriptor, "GetImplementationType");
        return HasAnyMemberAttribute<InjectionAttribute>(method);
    }

    /// <summary>
    /// 调用方法
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="methodName">方法名</param>
    /// <typeparam name="T">动态类型</typeparam>
    /// <returns></returns>
    private static T InvokeMethod<T>(object instance, string methodName) => (T)instance.GetType().GetMethod(methodName, BindingFlags)?.Invoke(instance, null)!;

    /// <summary>
    /// 是否有一个成员特性
    /// </summary>
    /// <param name="type">成员类型</param>
    /// <typeparam name="TAttribute">特性</typeparam>
    /// <returns></returns>
    private static bool HasAnyMemberAttribute<TAttribute>(Type type) where TAttribute : Attribute =>
        type.GetMembers(BindingFlags).Any(m => m.HasAttribute<TAttribute>()) || (type.BaseType != null && HasAnyMemberAttribute<TAttribute>(type.BaseType));

    /// <summary>
    /// 创建工厂
    /// </summary>
    /// <param name="descriptor"></param>
    /// <returns></returns>
    private Func<IServiceProvider, object> CreateFactory(ServiceDescriptor descriptor) => _ => _propertyInjector.InjectProperties(CreateInstance(descriptor));

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <param name="des"></param>
    /// <returns></returns>
    private object CreateInstance(ServiceDescriptor des) => des.ImplementationInstance ?? des.ImplementationFactory?.Invoke(this) ?? ActivatorUtilities.CreateInstance(this, des.ImplementationType!);
}
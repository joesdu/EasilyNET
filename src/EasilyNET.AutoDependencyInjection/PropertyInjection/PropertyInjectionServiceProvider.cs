using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <inheritdoc />
internal sealed class PropertyInjectionServiceProvider : IPropertyInjectionServiceProvider
{
    private readonly IPropertyInjector _propertyInjector;
    private readonly IServiceProvider _serviceProvider;

    public PropertyInjectionServiceProvider(IServiceCollection service)
    {
        ArgumentNullException.ThrowIfNull(service, nameof(service));
        service.AddSingleton<IPropertyInjectionServiceProvider>(this);
        _serviceProvider = service.BuildServiceProvider();
        _propertyInjector = new PropertyInjector(this);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        var instance = _serviceProvider.GetService(serviceType);
        return instance is null ? null : _propertyInjector.InjectProperties(instance);
    }

    /// <inheritdoc />
    public object GetRequiredService(Type serviceType)
    {
        var service = GetService(serviceType);
        ArgumentNullException.ThrowIfNull(service);
        return service;
    }
}
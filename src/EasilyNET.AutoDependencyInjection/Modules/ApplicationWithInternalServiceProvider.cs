using EasilyNET.AutoDependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace EasilyNET.AutoDependencyInjection.Modules;

internal class ApplicationWithInternalServiceProvider : ModuleApplicationBase, IApplicationWithInternalServiceProvider
{
    public ApplicationWithInternalServiceProvider(Type startupModuleType) : this(startupModuleType, new ServiceCollection()) { }

    private ApplicationWithInternalServiceProvider(
        [NotNull] Type startupModuleType,
        [NotNull] IServiceCollection services
    ) : base(startupModuleType,
        services)
    {
        Services.AddSingleton<IApplicationWithInternalServiceProvider>(this);
        ConfigureServices();
    }

    public IServiceScope? ServiceScope { get; private set; }

    public override void Dispose()
    {
        base.Dispose();
        ServiceScope?.Dispose();
    }

    public void Initialize()
    {
        CreateServiceProvider();
        InitializeModules();
    }

    public IServiceProvider CreateServiceProvider()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ServiceProvider != null)
        {
            return ServiceProvider;
        }
        ServiceScope = Services.BuildServiceProvider().CreateScope();
        SetServiceProvider(ServiceScope.ServiceProvider);
        return ServiceProvider!;
    }
}
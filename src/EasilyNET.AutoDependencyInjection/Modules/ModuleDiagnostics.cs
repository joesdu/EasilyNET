using System.Reflection;
using EasilyNET.AutoDependencyInjection.Abstractions;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <summary>
///     <para xml:lang="en">Default implementation of module diagnostics</para>
///     <para xml:lang="zh">模块诊断的默认实现</para>
/// </summary>
internal sealed class ModuleDiagnostics(IStartupModuleRunner runner, ServiceRegistry registry) : IModuleDiagnostics
{
    /// <inheritdoc />
    public IReadOnlyList<ModuleInfo> GetLoadedModules()
    {
        var modules = runner.Modules;
        var result = new List<ModuleInfo>(modules.Count);
        for (var i = 0; i < modules.Count; i++)
        {
            var module = modules[i];
            var moduleType = module.GetType();
            var dependencies = moduleType.GetCustomAttributes().OfType<IDependedTypesProvider>().SelectMany(p => p.GetDependedTypes()).Distinct().ToList();
            result.Add(new()
            {
                ModuleType = moduleType,
                Name = moduleType.Name,
                Order = i,
                Dependencies = dependencies
            });
        }
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<ServiceRegistrationInfo> GetAutoRegisteredServices()
    {
        var result = new List<ServiceRegistrationInfo>();
        // Add regular services
        foreach (var (serviceType, implType) in registry.GetAllImplementations())
        {
            result.Add(new()
            {
                ServiceType = serviceType,
                ImplementationType = implType,
                ServiceKey = null
            });
        }
        // Add keyed services
        foreach (var ((key, serviceType), descriptor) in registry.GetAllNamedServices())
        {
            result.Add(new()
            {
                ServiceType = serviceType,
                ImplementationType = descriptor.ImplementationType,
                ServiceKey = key
            });
        }
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ValidateModuleDependencies()
    {
        var issues = new List<string>();
        var loadedModuleTypes = runner.Modules.Select(m => m.GetType()).ToHashSet();
        foreach (var module in runner.Modules)
        {
            var moduleType = module.GetType();
            var dependencies = moduleType.GetCustomAttributes().OfType<IDependedTypesProvider>().SelectMany(p => p.GetDependedTypes()).Distinct();
            issues.AddRange(from dep in dependencies where !loadedModuleTypes.Contains(dep) select $"Module '{moduleType.Name}' depends on '{dep.Name}' which is not loaded (possibly disabled).");
        }
        return issues;
    }
}
using System.Reflection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc />
public class AppModule : IAppModule
{
    /// <inheritdoc />
    public virtual void ConfigureServices(ConfigureServicesContext context)
    {
        // Default implementation does nothing
    }

    /// <inheritdoc />
    public virtual Task ConfigureServicesAsync(ConfigureServicesContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual void ApplicationInitializationSync(ApplicationContext context)
    {
        // Default implementation does nothing
    }

    /// <inheritdoc />
    public virtual Task ApplicationInitialization(ApplicationContext context) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task ApplicationShutdown(ApplicationContext context) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual bool GetEnable(ConfigureServicesContext context) => true;

    /// <summary>
    ///     <para xml:lang="en">Get the dependent types of the module using topological sort to ensure correct order</para>
    ///     <para xml:lang="zh">使用拓扑排序获取模块的依赖类型，确保正确的顺序</para>
    /// </summary>
    /// <param name="moduleType">
    ///     <para xml:lang="en">Type of the module</para>
    ///     <para xml:lang="zh">模块的类型</para>
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     <para xml:lang="en">Thrown when circular dependency is detected</para>
    ///     <para xml:lang="zh">检测到循环依赖时抛出</para>
    /// </exception>
    public IEnumerable<Type> GetDependedTypes(Type? moduleType = null)
    {
        moduleType ??= GetType();
        var dependedTypes = moduleType.GetCustomAttributes().OfType<IDependedTypesProvider>().ToList();
        if (dependedTypes.Count == 0)
        {
            return [];
        }
        // Use recursive DFS with post-order collection to get correct topological order
        // Dependencies are processed in declaration order, and each dependency's sub-dependencies
        // are fully resolved before moving to the next dependency
        var result = new List<Type>();
        var visited = new HashSet<Type>();
        var visiting = new HashSet<Type>(); // O(1) cycle detection
        var path = new List<Type>();        // Track current path for error message only

        // Start from the module type
        Visit(moduleType);
        // Remove the module itself from the result as it will be added separately
        result.Remove(moduleType);
        return result;

        void Visit(Type type)
        {
            if (visited.Contains(type))
            {
                return;
            }
            if (!visiting.Add(type))
            {
                // Build the circular dependency chain message
                var cycleStart = path.IndexOf(type);
                var chainMessage = string.Join(" -> ", path.Skip(cycleStart).Select(t => t.Name).Append(type.Name));
                throw new InvalidOperationException($"Circular dependency detected: {chainMessage}. Module dependencies must form a directed acyclic graph (DAG).");
            }
            // Add to path before visiting dependencies (for error message)
            path.Add(type);
            // Get direct dependencies in declaration order
            var deps = type.GetCustomAttributes()
                           .OfType<IDependedTypesProvider>()
                           .SelectMany(p => p.GetDependedTypes())
                           .Where(t => t != type)
                           .Distinct()
                           .ToList();
            // Visit each dependency first (recursively)
            foreach (var dep in deps)
            {
                Visit(dep);
            }
            // After all dependencies are visited, add this type and remove from path
            path.RemoveAt(path.Count - 1); // O(1) removal from end
            visiting.Remove(type);
            if (visited.Add(type))
            {
                result.Add(type);
            }
        }
    }
}
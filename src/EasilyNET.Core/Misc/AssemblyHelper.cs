using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;

// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// Assembly helper class
/// </summary>
public static class AssemblyHelper
{
    private static readonly ConcurrentDictionary<string, Assembly?> AssemblyCache = new();
    private static readonly Lazy<IEnumerable<Assembly>> LazyAllAssemblies = new(LoadAssemblies);
    private static readonly Lazy<IEnumerable<Type>> LazyAllTypes = new(() => LoadTypes(LazyAllAssemblies.Value));

    /// <summary>
    /// Gets all assemblies that match the criteria
    /// </summary>
    public static IEnumerable<Assembly> AllAssemblies => LazyAllAssemblies.Value;

    /// <summary>
    /// Gets all types from the assemblies that match the criteria
    /// </summary>
    public static IEnumerable<Type> AllTypes => LazyAllTypes.Value;

    /// <summary>
    /// Gets assemblies by their names
    /// </summary>
    /// <param name="assemblyNames">Assembly FullName</param>
    /// <returns></returns>
    [RequiresUnreferencedCode("This method uses reflection and may not be compatible with AOT.")]
    public static IEnumerable<Assembly> GetAssembliesByName(params string[] assemblyNames)
    {
        return assemblyNames.Select(name =>
        {
            // ReSharper disable once InvertIf
            if (AssemblyCache.TryGetValue(name, out var assembly))
            {
                if (assembly is not null)
                {
                    return assembly;
                }
            }
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, $"{name}.dll"));
        });
    }

    /// <summary>
    /// Finds types that match the specified predicate
    /// </summary>
    public static IEnumerable<Type> FindTypes(Func<Type, bool> predicate) => AllTypes.Where(predicate);

    /// <summary>
    /// Finds all types marked with the specified attribute
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static IEnumerable<Type> FindTypesByAttribute<TAttribute>() where TAttribute : Attribute => FindTypesByAttribute(typeof(TAttribute));

    /// <summary>
    /// Finds all types marked with the specified attribute
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static IEnumerable<Type> FindTypesByAttribute<TAttribute>(Func<Type, bool> predicate) where TAttribute : Attribute => FindTypesByAttribute<TAttribute>().Where(predicate);

    /// <summary>
    /// Finds all types marked with the specified attribute
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<Type> FindTypesByAttribute(Type type) => AllTypes.Where(a => a.IsDefined(type, true)).Distinct();

    /// <summary>
    /// Finds assemblies that match the specified predicate
    /// </summary>
    public static IEnumerable<Assembly> FindAllItems(Func<Assembly, bool> predicate) => AllAssemblies.Where(predicate);

    private static IEnumerable<Type> LoadTypes(IEnumerable<Assembly> assemblies)
    {
        var types = new ConcurrentBag<Type>();
        Parallel.ForEach(assemblies, assembly =>
        {
            try
            {
                var typesInAssembly = assembly.GetTypes();
                types.AddRange(typesInAssembly);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load types from assembly: {assembly.FullName}, error: {ex.Message}");
            }
        });
        foreach (var item in types)
        {
            yield return item;
        }
    }

    private static IEnumerable<Assembly> LoadAssemblies()
    {
        var assemblies = DependencyContext.Default?.GetDefaultAssemblyNames() ?? [];
        var loadedAssemblies = new ConcurrentBag<Assembly>();
        Parallel.ForEach(assemblies, assembly => LoadAssembly(assembly, ref loadedAssemblies));
        foreach (var item in loadedAssemblies)
        {
            yield return item;
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Assembly))]
    private static void LoadAssembly(AssemblyName assemblyName, ref ConcurrentBag<Assembly> loadedAssemblies)
    {
        if (AssemblyCache.TryGetValue(assemblyName.FullName, out var cachedAssembly))
        {
            if (cachedAssembly is not null)
            {
                loadedAssemblies.Add(cachedAssembly);
            }
            return;
        }
        try
        {
            var assembly = Assembly.Load(assemblyName);
            if (loadedAssemblies.Contains(assembly)) return;
            AssemblyCache[assemblyName.FullName] = assembly;
            loadedAssemblies.Add(assembly);
        }
        catch (Exception ex)
        {
            AssemblyCache[assemblyName.FullName] = null;
            Debug.WriteLine($"Failed to load assembly: {assemblyName.Name}, error: {ex.Message}");
        }
    }
}
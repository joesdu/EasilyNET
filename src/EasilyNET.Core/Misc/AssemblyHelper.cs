using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;

// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Assembly helper class</para>
///     <para xml:lang="zh">程序集帮助类</para>
/// </summary>
public static class AssemblyHelper
{
    private static readonly ConcurrentDictionary<string, Assembly?> AssemblyCache = new();
    private static readonly Lazy<IEnumerable<Assembly>> LazyAllAssemblies = new(LoadAssemblies);
    private static readonly Lazy<IEnumerable<Type>> LazyAllTypes = new(() => LoadTypes(LazyAllAssemblies.Value));

    /// <summary>
    ///     <para xml:lang="en">Gets all assemblies that match the criteria</para>
    ///     <para xml:lang="zh">获取所有符合条件的程序集</para>
    /// </summary>
    public static IEnumerable<Assembly> AllAssemblies => LazyAllAssemblies.Value;

    /// <summary>
    ///     <para xml:lang="en">Gets all types from the assemblies that match the criteria</para>
    ///     <para xml:lang="zh">从符合条件的程序集获取所有类型</para>
    /// </summary>
    public static IEnumerable<Type> AllTypes => LazyAllTypes.Value;

    /// <summary>
    ///     <para xml:lang="en">Gets assemblies by their names</para>
    ///     <para xml:lang="zh">通过名称获取程序集</para>
    /// </summary>
    /// <param name="assemblyNames">
    ///     <para xml:lang="en">Assembly FullName</para>
    ///     <para xml:lang="zh">程序集全名</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A collection of assemblies</para>
    ///     <para xml:lang="zh">程序集集合</para>
    /// </returns>
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
    ///     <para xml:lang="en">Finds types that match the specified predicate</para>
    ///     <para xml:lang="zh">查找符合指定谓词的类型</para>
    /// </summary>
    /// <param name="predicate">
    ///     <para xml:lang="en">The predicate to match types</para>
    ///     <para xml:lang="zh">匹配类型的谓词</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A collection of types</para>
    ///     <para xml:lang="zh">类型集合</para>
    /// </returns>
    public static IEnumerable<Type> FindTypes(Func<Type, bool> predicate) => AllTypes.Where(predicate);

    /// <summary>
    ///     <para xml:lang="en">Finds all types marked with the specified attribute</para>
    ///     <para xml:lang="zh">查找所有标有指定属性的类型</para>
    /// </summary>
    /// <typeparam name="TAttribute">
    ///     <para xml:lang="en">The attribute type</para>
    ///     <para xml:lang="zh">属性类型</para>
    /// </typeparam>
    /// <returns>
    ///     <para xml:lang="en">A collection of types</para>
    ///     <para xml:lang="zh">类型集合</para>
    /// </returns>
    public static IEnumerable<Type> FindTypesByAttribute<TAttribute>() where TAttribute : Attribute => FindTypesByAttribute(typeof(TAttribute));

    /// <summary>
    ///     <para xml:lang="en">Finds all types marked with the specified attribute</para>
    ///     <para xml:lang="zh">查找所有标有指定属性的类型</para>
    /// </summary>
    /// <typeparam name="TAttribute">
    ///     <para xml:lang="en">The attribute type</para>
    ///     <para xml:lang="zh">属性类型</para>
    /// </typeparam>
    /// <param name="predicate">
    ///     <para xml:lang="en">The predicate to match types</para>
    ///     <para xml:lang="zh">匹配类型的谓词</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A collection of types</para>
    ///     <para xml:lang="zh">类型集合</para>
    /// </returns>
    public static IEnumerable<Type> FindTypesByAttribute<TAttribute>(Func<Type, bool> predicate) where TAttribute : Attribute => FindTypesByAttribute<TAttribute>().Where(predicate);

    /// <summary>
    ///     <para xml:lang="en">Finds all types marked with the specified attribute</para>
    ///     <para xml:lang="zh">查找所有标有指定属性的类型</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The attribute type</para>
    ///     <para xml:lang="zh">属性类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A collection of types</para>
    ///     <para xml:lang="zh">类型集合</para>
    /// </returns>
    public static IEnumerable<Type> FindTypesByAttribute(Type type) => AllTypes.Where(a => a.IsDefined(type, true)).Distinct();

    /// <summary>
    ///     <para xml:lang="en">Finds assemblies that match the specified predicate</para>
    ///     <para xml:lang="zh">查找符合指定谓词的程序集</para>
    /// </summary>
    /// <param name="predicate">
    ///     <para xml:lang="en">The predicate to match assemblies</para>
    ///     <para xml:lang="zh">匹配程序集的谓词</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A collection of assemblies</para>
    ///     <para xml:lang="zh">程序集集合</para>
    /// </returns>
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
        var assemblies = DependencyContext.Default?.GetRuntimeAssemblyNames(AppContext.BaseDirectory) ?? [];
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
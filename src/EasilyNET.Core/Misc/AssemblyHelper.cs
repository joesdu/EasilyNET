using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// 程序集帮助类
/// </summary>
public static class AssemblyHelper
{
    private static readonly ConcurrentDictionary<string, Assembly?> AssemblyCache = [];

    /// <summary>
    /// 构造函数
    /// </summary>
    static AssemblyHelper()
    {
        AllAssemblies = LoadAssemblies();
        AllTypes = LoadTypes(AllAssemblies);
    }

    /// <summary>
    /// 获取所有扫描到符合条件的程序集
    /// </summary>
    public static IEnumerable<Assembly> AllAssemblies { get; }

    /// <summary>
    /// 获取所有扫描到符合条件的程序集中的类型
    /// </summary>
    public static IEnumerable<Type> AllTypes { get; }

    /// <summary>
    /// 根据程序集名字得到程序集
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
    /// 查找指定条件的类型
    /// </summary>
    public static IEnumerable<Type> FindTypes(Func<Type, bool> predicate) => AllTypes.Where(predicate);

    /// <summary>
    /// 查找所有指定特性标记的类型
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static IEnumerable<Type> FindTypesByAttribute<TAttribute>() where TAttribute : Attribute => FindTypesByAttribute(typeof(TAttribute));

    /// <summary>
    /// 查找所有指定特性标记的类型
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static IEnumerable<Type> FindTypesByAttribute<TAttribute>(Func<Type, bool> predicate) where TAttribute : Attribute => FindTypesByAttribute<TAttribute>().Where(predicate);

    /// <summary>
    /// 查找所有指定特性标记的类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<Type> FindTypesByAttribute(Type type) => AllTypes.Where(a => a.IsDefined(type, true)).Distinct();

    /// <summary>
    /// 查找指定条件的类型
    /// </summary>
    public static IEnumerable<Assembly> FindAllItems(Func<Assembly, bool> predicate) => AllAssemblies.Where(predicate);

    private static IEnumerable<Type> LoadTypes(IEnumerable<Assembly> assemblies)
    {
        var types = new ConcurrentBag<Type>();
        Parallel.ForEach(assemblies, assembly => types.AddRange(assembly.GetTypes()));
        foreach (var item in types)
        {
            yield return item;
        }
    }

    private static IEnumerable<Assembly> LoadAssemblies()
    {
        var start = Stopwatch.GetTimestamp();
        var assemblies = DependencyContext.Default?.GetDefaultAssemblyNames().ToHashSet() ?? [];
        var loadedAssemblies = new ConcurrentBag<Assembly>();
        Parallel.ForEach(assemblies, assembly => LoadAssembly(assembly, ref loadedAssemblies));
        // 该函数对整体性能影响较大,输出执行时间,便于性能分享
        Console.WriteLine($"Load assemblies elapsed time: {Stopwatch.GetElapsedTime(start, Stopwatch.GetTimestamp()).TotalMilliseconds}ms");
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
            _ = assembly.GetTypes(); // Pre-check types to ensure they can be loaded
            loadedAssemblies.Add(assembly);
        }
        catch (Exception ex)
        {
            AssemblyCache[assemblyName.FullName] = null;
            Debug.WriteLine($"Failed to load assembly: {assemblyName.Name}, error: {ex.Message}");
        }
    }
}
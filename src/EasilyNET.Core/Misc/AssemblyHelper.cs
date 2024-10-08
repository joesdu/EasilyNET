using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
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
        AllTypes = AllAssemblies.SelectMany(c => c.GetTypes());
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
    /// <param name="assemblyNames"></param>
    /// <returns></returns>
    public static IEnumerable<Assembly> GetAssembliesByName(params string[] assemblyNames)
    {
        return assemblyNames.Select(name => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, $"{name}.dll")));
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
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<Type> FindTypesByAttribute(Type type) => AllTypes.Where(a => a.IsDefined(type, true)).Distinct();

    /// <summary>
    /// 查找指定条件的类型
    /// </summary>
    public static IEnumerable<Assembly> FindAllItems(Func<Assembly, bool> predicate) => AllAssemblies.Where(predicate);

    private static IEnumerable<Assembly> LoadAssemblies()
    {
        var assemblyNames = DependencyContext.Default?.GetDefaultAssemblyNames().Where(c => c.Name is not null).ToFrozenSet();
        if (assemblyNames is null) yield break;
        var batchSize = (int)Math.Ceiling((double)assemblyNames.Count / Environment.ProcessorCount); // 计算每组的大小
        var loadedAssemblies = new ConcurrentBag<Assembly>();
        if (assemblyNames.Count >= Environment.ProcessorCount)
        {
            var batches = assemblyNames.Select((name, index) => new { name, index })
                                       .GroupBy(x => x.index / batchSize)
                                       .Select(g => g.Select(x => x.name));
            Parallel.ForEach(batches, batch =>
            {
                foreach (var assemblyName in batch)
                {
                    LoadAssembly(assemblyName, loadedAssemblies);
                }
            });
        }
        else
        {
            foreach (var assemblyName in assemblyNames)
            {
                LoadAssembly(assemblyName, loadedAssemblies);
            }
        }
        foreach (var assembly in loadedAssemblies)
        {
            yield return assembly;
        }
    }

    private static void LoadAssembly(AssemblyName assemblyName, ConcurrentBag<Assembly> loadedAssemblies)
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
            AssemblyCache[assemblyName.FullName] = assembly;
            // 预检查程序集中的类型，确保可以加载
            _ = assembly.GetTypes();
            loadedAssemblies.Add(assembly);
        }
        catch (ReflectionTypeLoadException)
        {
            AssemblyCache[assemblyName.FullName] = null;
            Debug.WriteLine($"无法加载程序集中的某些类型: {assemblyName.Name}, 将跳过.");
        }
        catch (Exception ex)
        {
            AssemblyCache[assemblyName.FullName] = null;
            Debug.WriteLine($"加载程序集失败: {assemblyName.Name}, 错误: {ex.Message}");
        }
    }
}
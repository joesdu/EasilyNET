using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyModel;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Assembly helper class</para>
///     <para xml:lang="zh">程序集帮助类</para>
/// </summary>
public static class AssemblyHelper
{
    private static readonly ConcurrentDictionary<string, Assembly?> AssemblyCache = new();
    private static readonly Lazy<IEnumerable<Assembly>> LazyAllAssemblies = new(() => LoadAssemblies(LoadFromAllDll));
    private static readonly Lazy<IEnumerable<Type>> LazyAllTypes = new(() => LoadTypes(LazyAllAssemblies.Value));

    static AssemblyHelper()
    {
        var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
        if (!string.IsNullOrEmpty(entryAssemblyName))
        {
            AssemblyNames.Add(entryAssemblyName);
        }
    }

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
    ///     <para xml:lang="en">
    ///     Indicates whether to load all assemblies. Loading all assemblies may be slow. For general use, it defaults to
    ///     <see langword="true" />. It is recommended to set it to <see langword="false" /> and manually specify the assemblies to load using
    ///     <see cref="AddAssemblyNames" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     是否获取所有的程序集。加载所有程序集可能会比较慢。为保持通用性，默认为 <see langword="true" />。推荐设置为 <see langword="false" /> 并手动通过
    ///     <see cref="AddAssemblyNames" /> 指定需要加载的程序集。
    ///     </para>
    /// </summary>
    public static bool LoadFromAllDll { get; set; } = true;

    private static List<string> AssemblyNames { get; } = ["EasilyNET*"];

    /// <summary>
    ///     <para xml:lang="en">Adds assembly names to be loaded when <see cref="LoadFromAllDll" /> is set to <see langword="false" />.</para>
    ///     <para xml:lang="zh">当 <see cref="LoadFromAllDll" /> 设置为 <see langword="false" /> 时，添加要加载的程序集名称。</para>
    /// </summary>
    /// <param name="names">
    ///     <para xml:lang="en">The names of the assemblies to add.</para>
    ///     <para xml:lang="zh">要添加的程序集名称。</para>
    /// </param>
    public static void AddAssemblyNames(params IEnumerable<string> names) => AssemblyNames.AddRange(names);

    /// <summary>
    ///     <para xml:lang="en">Gets assemblies by their names</para>
    ///     <para xml:lang="zh">通过名称获取程序集</para>
    /// </summary>
    /// <param name="assemblyNames">
    ///     <para xml:lang="en">Assembly name</para>
    ///     <para xml:lang="zh">程序集名称,支持通配符匹配,如: EasilyNET* </para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A collection of assemblies</para>
    ///     <para xml:lang="zh">程序集集合</para>
    /// </returns>
    [RequiresUnreferencedCode("This method uses reflection and may not be compatible with AOT.")]
    public static IEnumerable<Assembly> GetAssembliesByName(params IEnumerable<string> assemblyNames)
    {
        var regexPatterns = assemblyNames.Select(name =>
                                             new Regex($"^{Regex.Escape(name)
                                                                .Replace("\\*", ".*", StringComparison.OrdinalIgnoreCase)
                                                                .Replace("\\?", ".", StringComparison.OrdinalIgnoreCase)}$", RegexOptions.IgnoreCase))
                                         .ToFrozenSet();
        var allAssemblyFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.dll", SearchOption.AllDirectories);
        var matchingAssemblies = allAssemblyFiles.Where(file =>
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            return regexPatterns.Any(pattern => pattern.IsMatch(fileName));
        });
        return matchingAssemblies.Select(file =>
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (AssemblyCache.TryGetValue(name, out var assembly) && assembly is not null)
            {
                return assembly;
            }
            var loadedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
            AssemblyCache[name] = loadedAssembly;
            return loadedAssembly;
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

    private static IEnumerable<Assembly> LoadAssemblies(bool fromAll)
    {
        var assemblies = fromAll ? DependencyContext.Default?.GetRuntimeAssemblyNames(AppContext.BaseDirectory) ?? [] : GetAssembliesByName(AssemblyNames).Select(c => c.GetName());
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
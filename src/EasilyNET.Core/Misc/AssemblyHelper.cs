using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// 程序集帮助类
/// </summary>
// ReSharper disable once UnusedType.Global
public static class AssemblyHelper
{
    private static readonly HashSet<string> Filters = ["dotnet-", "Microsoft.", "mscorlib", "netstandard", "System", "Windows"];
    private static readonly HashSet<Assembly>? _allAssemblies;
    private static readonly HashSet<Type>? _allTypes;

    /// <summary>
    /// 需要排除的项目
    /// </summary>
    private static readonly HashSet<string> FilterLibs = [];

    /// <summary>
    /// 构造函数
    /// </summary>
    static AssemblyHelper()
    {
        _allAssemblies = DependencyContext.Default?.GetDefaultAssemblyNames().Where(c => c.Name is not null && !Filters.Any(c.Name.StartsWith) && !FilterLibs.Any(c.Name.StartsWith)).Select(Assembly.Load).ToHashSet();
        _allTypes = _allAssemblies?.SelectMany(c => c.GetTypes()).ToHashSet();
    }

    /// <summary>
    /// 添加排除项目,该排除项目可能会影响AutoDependenceInjection自动注入,请使用的时候自行测试.
    /// </summary>
    /// <param name="names"></param>
    public static void AddExcludeLibs(params string[] names) => FilterLibs.AddRangeIfNotContains(names);

    /// <summary>
    /// 根据程序集名字得到程序集
    /// </summary>
    /// <param name="assemblyNames"></param>
    /// <returns></returns>
    public static HashSet<Assembly> GetAssembliesByName(params string[] assemblyNames) => assemblyNames.Select(o => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, $"{o}.dll"))).ToHashSet();

    /// <summary>
    /// 查找指定条件的类型
    /// </summary>
    public static HashSet<Type> FindTypes(Func<Type, bool> predicate) => _allTypes!.Where(predicate).ToHashSet();

    /// <summary>
    /// 查找所有指定特性标记的类型
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static HashSet<Type> FindTypesByAttribute<TAttribute>() where TAttribute : Attribute => FindTypesByAttribute(typeof(TAttribute));

    /// <summary>
    /// 查找所有指定特性标记的类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static HashSet<Type> FindTypesByAttribute(Type type) => _allTypes!.Where(a => a.IsDefined(type, true)).Distinct().ToHashSet();

    /// <summary>
    /// 查找指定条件的类型
    /// </summary>
    public static HashSet<Assembly> FindAllItems(Func<Assembly, bool> predicate) => _allAssemblies!.Where(predicate).ToHashSet();
}
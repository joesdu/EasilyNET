using Microsoft.Extensions.DependencyModel;
using System.Reflection;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.AutoDependencyInjection.Extensions;

/// <summary>
/// 程序集帮助类
/// </summary>
internal static class AssemblyHelper
{
    private static readonly string[] Filters = { "dotnet-", "Microsoft.", "mscorlib", "netstandard", "System", "Windows" };
    private static readonly IEnumerable<Type>? _allTypes;

    /// <summary>
    /// 构造函数
    /// </summary>
    static AssemblyHelper()
    {
        var allAssemblies = DependencyContext.Default?.GetDefaultAssemblyNames().Where(c => c.Name is not null && !Filters.Any(c.Name.StartsWith)).Select(Assembly.Load);
        _allTypes = allAssemblies?.SelectMany(c => c.GetTypes());
    }

    /// <summary>
    /// 查找指定条件的类型
    /// </summary>
    internal static IEnumerable<Type> FindTypes(Func<Type, bool> predicate) => _allTypes!.Where(predicate).ToArray();
}
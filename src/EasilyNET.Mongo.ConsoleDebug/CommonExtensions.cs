using System.Collections.Frozen;
using System.Reflection;

namespace EasilyNET.Mongo.ConsoleDebug;

internal static class CommonExtensions
{
    internal static readonly FrozenSet<string> CommandsWithCollectionNameAsValue =
        new HashSet<string>
        {
            "aggregate",
            "count",
            "distinct",
            "mapReduce",
            "geoSearch",
            "delete",
            "find",
            "killCursors",
            "findAndModify",
            "insert",
            "update",
            "create",
            "drop",
            "createIndexes",
            "listIndexes"
        }.ToFrozenSet();

    /// <summary>
    /// 获取指定类型的程序集版本.
    /// </summary>
    /// <typeparam name="T">类型.</typeparam>
    /// <returns>程序集版本.</returns>
    internal static string GetVersion<T>() => typeof(T).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? string.Empty;
}
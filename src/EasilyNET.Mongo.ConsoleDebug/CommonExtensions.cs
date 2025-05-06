using System.Collections.Frozen;
using System.Reflection;

namespace EasilyNET.Mongo.ConsoleDebug;

/// <summary>
///     <para xml:lang="en">Common extensions</para>
///     <para xml:lang="zh">通用扩展</para>
/// </summary>
internal static class CommonExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Commands with collection name as value</para>
    ///     <para xml:lang="zh">以集合名称为值的命令</para>
    /// </summary>
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
    ///     <para xml:lang="en">Get the assembly version of the specified type.</para>
    ///     <para xml:lang="zh">获取指定类型的程序集版本。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">Type.</para>
    ///     <para xml:lang="zh">类型。</para>
    /// </typeparam>
    internal static string GetVersion<T>() => typeof(T).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? string.Empty;
}
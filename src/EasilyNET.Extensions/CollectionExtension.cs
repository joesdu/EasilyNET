using System.Collections;
using System.Text;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Extensions;

/// <summary>
/// 集合扩展
/// </summary>
public static class CollectionExtension
{
    /// <summary>
    /// 把集合转成SqlIn
    /// </summary>
    /// <typeparam name="TSource">源</typeparam>
    /// <param name="values">要转换的值</param>
    /// <param name="separator">分割符</param>
    /// <param name="left">左边符</param>
    /// <param name="right">右边符</param>
    /// <returns>返回组装好的值，例如"'a','b'"</returns>
    public static string ToSqlIn<TSource>(this IEnumerable<TSource> values, string separator = ",", string left = "'", string right = "'")
    {
        StringBuilder sb = new();
        var enumerable = values as TSource[] ?? values.ToArray();
        if (!enumerable.Any())
        {
            return string.Empty;
        }
        enumerable.ToList().ForEach(o => _ = sb.Append($"{left}{o}{right}{separator}"));
        return sb.ToString().TrimEnd($"{separator}".ToCharArray());
    }

    /// <summary>
    /// WhereIf 扩展,可显著减少if的使用
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    public static IEnumerable<TSource> WhereIf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, bool condition) where TSource : IEnumerable => condition ? source.Where(predicate) : source;

    /// <summary>
    /// 将列表转换为树形结构（泛型无限递归）
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="list">数据</param>
    /// <param name="rootWhere">根条件</param>
    /// <param name="childrenWhere">节点条件</param>
    /// <param name="addChildren">添加子节点</param>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static List<T> ToTree<T>(this List<T> list, Func<T, T, bool> rootWhere, Func<T, T, bool> childrenWhere, Action<T, IEnumerable<T>> addChildren, T? entity = default)
    {
        var treeList = new List<T>();
        //空树
        if (list.Count == 0)
        {
            return treeList;
        }
        if (!list.Any(e => rootWhere(entity!, e)))
        {
            return treeList;
        }
        //树根
        if (list.Any(e => rootWhere(entity!, e)))
        {
            treeList.AddRange(list.Where(e => rootWhere(entity!, e)));
        }
        //树叶
        foreach (var item in treeList)
        {
            if (!list.Any(e => childrenWhere(item, e))) continue;
            var nodeData = list.Where(e => childrenWhere(item, e)).ToList();
            foreach (var child in nodeData)
            {
                //添加子集
                var data = list.ToTree(childrenWhere, childrenWhere, addChildren, child);
                addChildren(child, data);
            }
            addChildren(item, nodeData);
        }
        return treeList;
    }
}
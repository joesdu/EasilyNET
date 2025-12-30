using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">IEnumerableExtensions</para>
///     <para xml:lang="zh">IEnumerable扩展方法</para>
/// </summary>
public static class IEnumerableExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Standard deviation</para>
    ///     <para xml:lang="zh">标准差</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    public static T? StandardDeviation<T>(this IEnumerable<T> source) where T : IConvertible
    {
        return source.Select(t => t.ConvertTo<double>()).StandardDeviation().ConvertTo<T>();
    }

    /// <summary>
    ///     <para xml:lang="en">Standard deviation</para>
    ///     <para xml:lang="zh">标准差</para>
    /// </summary>
    /// <param name="source"></param>
    public static double StandardDeviation(this IEnumerable<double> source)
    {
        double count = 0;
        double mean = 0;
        double m2 = 0;
        foreach (var value in source)
        {
            count++;
            var delta = value - mean;
            mean += delta / count;
            m2 += delta * (value - mean);
        }
        // 使用样本标准差（Bessel 校正），当 count <= 1 时返回 0 保持既有行为
        return count <= 1 ? 0 : Math.Sqrt(m2 / (count - 1));
    }

    /// <summary>
    ///     <para xml:lang="en">Declare collection as non-null</para>
    ///     <para xml:lang="zh">将集合声明为非null集合</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> AsNotNull<T>(this List<T>? list) => list ?? [];

    private static void ChangeIndexInternal<T>(IList<T> list, T item, int index)
    {
        index = Math.Max(0, index);
        index = Math.Min(list.Count - 1, index);
        list.Remove(item);
        list.Insert(index, item);
    }

    /// <summary>
    ///     <para xml:lang="en">Convert list to tree structure (generic infinite recursion)</para>
    ///     <para xml:lang="zh">将列表转换为树形结构（泛型无限递归）</para>
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="list">数据</param>
    /// <param name="rootWhere">根条件</param>
    /// <param name="childrenWhere">节点条件</param>
    /// <param name="addChildren">添加子节点</param>
    /// <param name="entity"></param>
    public static List<T> ToTree<T>(this List<T> list, Func<T, T, bool> rootWhere, Func<T, T, bool> childrenWhere, Action<T, IEnumerable<T>> addChildren, T? entity = default)
    {
        var treeList = new List<T>();
        if (list.Count == 0)
        {
            return treeList;
        }
        var roots = list.Where(e => rootWhere(entity!, e)).ToList();
        if (roots.Count == 0)
        {
            return treeList;
        }
        treeList.AddRange(roots);
        foreach (var item in treeList)
        {
            var nodeData = list.Where(e => childrenWhere(item, e)).ToList();
            if (nodeData.Count == 0)
            {
                continue;
            }
            foreach (var child in nodeData)
            {
                var data = list.ToTree(childrenWhere, childrenWhere, addChildren, child);
                addChildren(child, data);
            }
            addChildren(item, nodeData);
        }
        return treeList;
    }

    /// <param name="items">The enumerable to search.</param>
    /// <typeparam name="T"></typeparam>
    extension<T>(IEnumerable<T> items)
    {
        /// <summary>
        /// Finds the index of the first item matching an expression in an enumerable.
        /// </summary>
        /// <param name="predicate">The expression to test the items against.</param>
        /// <returns>The index of the first matching item, or -1 if no items match.</returns>
        /// <exception cref="ArgumentNullException">
        /// items
        /// or
        /// predicate
        /// </exception>
        public int IndexOf(Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(predicate);
            var retVal = 0;
            foreach (var item in items)
            {
                if (predicate(item))
                {
                    return retVal;
                }
                retVal++;
            }
            return -1;
        }

        /// <summary>
        /// Finds the index of the first item matching an expression in an enumerable.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null. </exception>
        /// <param name="predicate">The expression to test the items against. </param>
        /// <returns>
        /// The index of the first matching item, or -1 if no items match.
        /// </returns>
        public int IndexOf(Func<T, int, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(predicate);
            var retVal = 0;
            foreach (var item in items)
            {
                if (predicate(item, retVal))
                {
                    return retVal;
                }
                retVal++;
            }
            return -1;
        }

        /// <summary>
        /// Finds the index of the first occurence of an item in an enumerable.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The index of the first matching item, or -1 if the item was not found.</returns>
        public int IndexOf(T item)
        {
            return items.IndexOf(i => EqualityComparer<T>.Default.Equals(i, item));
        }
    }

    /// <param name="first"></param>
    /// <typeparam name="TFirst"></typeparam>
    extension<TFirst>(IEnumerable<TFirst>? first)
    {
        /// <summary>
        ///     <para xml:lang="en">Get intersection by field property</para>
        ///     <para xml:lang="zh">按字段属性判等取交集</para>
        /// </summary>
        /// <typeparam name="TSecond"></typeparam>
        /// <param name="second"></param>
        /// <param name="condition"></param>
        public IEnumerable<TFirst> IntersectBy<TSecond>(IEnumerable<TSecond> second, Func<TFirst, TSecond, bool> condition)
        {
            return first.AsNotNull().Where(f => second.Any(s => condition(f, s)));
        }

        /// <summary>
        ///     <para xml:lang="en">Get intersection by field property</para>
        ///     <para xml:lang="zh">按字段属性判等取交集</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="second"></param>
        /// <param name="keySelector"></param>
        public IEnumerable<TFirst> IntersectBy<TKey>(IEnumerable<TFirst> second, Func<TFirst, TKey> keySelector) => first.AsNotNull().IntersectBy(second.Select(keySelector), keySelector);

        /// <summary>
        ///     <para xml:lang="en">Get intersection by field property</para>
        ///     <para xml:lang="zh">按字段属性判等取交集</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="second"></param>
        /// <param name="keySelector"></param>
        /// <param name="comparer"></param>
        public IEnumerable<TFirst> IntersectBy<TKey>(IEnumerable<TFirst> second, Func<TFirst, TKey> keySelector, IEqualityComparer<TKey>? comparer) => first.AsNotNull().IntersectBy(second.Select(keySelector), keySelector, comparer);

        /// <summary>
        ///     <para xml:lang="en">Get difference by field property</para>
        ///     <para xml:lang="zh">按字段属性判等取差集</para>
        /// </summary>
        /// <typeparam name="TSecond"></typeparam>
        /// <param name="second"></param>
        /// <param name="condition"></param>
        public IEnumerable<TFirst> ExceptBy<TSecond>(IEnumerable<TSecond> second, Func<TFirst, TSecond, bool> condition)
        {
            return first.AsNotNull().Where(f => !second.Any(s => condition(f, s)));
        }

        /// <summary>
        ///     <para xml:lang="en">Remove duplicates by field</para>
        ///     <para xml:lang="zh">按字段去重</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        public IEnumerable<TFirst> DistinctBy<TKey>(Func<TFirst, TKey> keySelector)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(keySelector);
            return Enumerable.DistinctBy(first, keySelector);
        }

        /// <summary>
        ///     <para xml:lang="en">Get intersection by field property</para>
        ///     <para xml:lang="zh">按字段属性判等取交集</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="second"></param>
        /// <param name="keySelector"></param>
        public IEnumerable<TFirst> IntersectBy<TKey>(IEnumerable<TKey> second, Func<TFirst, TKey> keySelector) => Enumerable.IntersectBy(first.AsNotNull(), second, keySelector);

        /// <summary>
        ///     <para xml:lang="en">Get intersection by field property</para>
        ///     <para xml:lang="zh">按字段属性判等取交集</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="second"></param>
        /// <param name="keySelector"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public IEnumerable<TFirst> IntersectBy<TKey>(IEnumerable<TKey> second, Func<TFirst, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(keySelector);
            return Enumerable.IntersectBy(first, second, keySelector, comparer);
        }

        /// <summary>
        ///     <para xml:lang="en">Get difference by field property</para>
        ///     <para xml:lang="zh">按字段属性判等取差集</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="second"></param>
        /// <param name="keySelector"></param>
        public IEnumerable<TFirst> ExceptBy<TKey>(IEnumerable<TKey> second, Func<TFirst, TKey> keySelector) => Enumerable.ExceptBy(first.AsNotNull(), second, keySelector);

        /// <summary>
        ///     <para xml:lang="en">Get difference by field property</para>
        ///     <para xml:lang="zh">按字段属性判等取差集</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="second"></param>
        /// <param name="keySelector"></param>
        /// <param name="comparer"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public IEnumerable<TFirst> ExceptBy<TKey>(IEnumerable<TKey> second, Func<TFirst, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(keySelector);
            return Enumerable.ExceptBy(first, second, keySelector, comparer);
        }

        /// <summary>
        ///     <para xml:lang="en">Convert to HashSet</para>
        ///     <para xml:lang="zh">转HashSet</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        public HashSet<TResult> ToHashSet<TResult>(Func<TFirst, TResult> selector) => [.. first.AsNotNull().Select(selector)];

        /// <summary>
        ///     <para xml:lang="en">Iterate IEnumerable</para>
        ///     <para xml:lang="zh">遍历IEnumerable</para>
        /// </summary>
        /// <param name="action">回调方法</param>
        public void ForEach(Action<TFirst> action)
        {
            foreach (var o in first.AsNotNull())
            {
                action(o);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Asynchronous foreach</para>
        ///     <para xml:lang="zh">异步foreach</para>
        /// </summary>
        /// <param name="maxParallelCount">最大并行数</param>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        public Task ForeachAsync(Func<TFirst, Task> action, int maxParallelCount, CancellationToken cancellationToken = default)
        {
            if (Debugger.IsAttached)
            {
                maxParallelCount = 1;
            }
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallelCount,
                CancellationToken = cancellationToken
            };
            return Parallel.ForEachAsync(first.AsNotNull(), options, async (item, _) => await action(item));
        }

        /// <summary>
        ///     <para xml:lang="en">Asynchronous foreach</para>
        ///     <para xml:lang="zh">异步foreach</para>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        public Task ForeachAsync(Func<TFirst, Task> action, CancellationToken cancellationToken = default)
        {
            if (first is ICollection<TFirst> collection)
            {
                return collection.ForeachAsync(action, collection.Count, cancellationToken);
            }
            var list = first.AsNotNull().ToList();
            return list.ForeachAsync(action, list.Count, cancellationToken);
        }

        /// <summary>
        ///     <para xml:lang="en">Asynchronous Select</para>
        ///     <para xml:lang="zh">异步Select</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        public Task<TResult[]> SelectAsync<TResult>(Func<TFirst, Task<TResult>> selector) => Task.WhenAll(first.AsNotNull().Select(selector));

        /// <summary>
        ///     <para xml:lang="en">Asynchronous Select</para>
        ///     <para xml:lang="zh">异步Select</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        public Task<TResult[]> SelectAsync<TResult>(Func<TFirst, int, Task<TResult>> selector) => Task.WhenAll(first.AsNotNull().Select(selector));

        /// <summary>
        ///     <para xml:lang="en">Asynchronous Select</para>
        ///     <para xml:lang="zh">异步Select</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="maxParallelCount">最大并行数</param>
        public async Task<List<TResult>> SelectAsync<TResult>(Func<TFirst, Task<TResult>> selector, int maxParallelCount)
        {
            var source = first.AsNotNull().ToList();
            if (source.Count == 0)
            {
                return [];
            }
            var results = new TResult[source.Count];
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallelCount };
            await Parallel.ForEachAsync(Enumerable.Range(0, source.Count), options, async (i, _) => results[i] = await selector(source[i]));
            return [.. results];
        }

        /// <summary>
        ///     <para xml:lang="en">Asynchronous Select</para>
        ///     <para xml:lang="zh">异步Select</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="maxParallelCount">最大并行数</param>
        public async Task<List<TResult>> SelectAsync<TResult>(Func<TFirst, int, Task<TResult>> selector, int maxParallelCount)
        {
            var source = first.AsNotNull().ToList();
            if (source.Count == 0)
            {
                return [];
            }
            var results = new TResult[source.Count];
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallelCount };
            await Parallel.ForEachAsync(Enumerable.Range(0, source.Count), options, async (i, _) => results[i] = await selector(source[i], i));
            return [.. results];
        }

        /// <summary>
        ///     <para xml:lang="en">Asynchronous For</para>
        ///     <para xml:lang="zh">异步For</para>
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="maxParallelCount">最大并行数</param>
        /// <param name="cancellationToken">取消口令</param>
        public Task ForAsync(Func<TFirst, int, Task> selector, int maxParallelCount, CancellationToken cancellationToken = default)
        {
            if (Debugger.IsAttached)
            {
                maxParallelCount = 1;
            }
            var source = first.AsNotNull().ToList();
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallelCount,
                CancellationToken = cancellationToken
            };
            return Parallel.ForEachAsync(Enumerable.Range(0, source.Count), options, async (i, _) => await selector(source[i], i));
        }

        /// <summary>
        ///     <para xml:lang="en">Asynchronous For</para>
        ///     <para xml:lang="zh">异步For</para>
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="cancellationToken">取消口令</param>
        public Task ForAsync(Func<TFirst, int, Task> selector, CancellationToken cancellationToken = default)
        {
            if (first is ICollection<TFirst> collection)
            {
                return collection.ForAsync(selector, collection.Count, cancellationToken);
            }
            var list = first.AsNotNull().ToList();
            return list.ForAsync(selector, list.Count, cancellationToken);
        }

        /// <summary>
        ///     <para xml:lang="en">Get maximum value</para>
        ///     <para xml:lang="zh">取最大值</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="defaultValue"></param>
        public TResult? MaxOrDefault<TResult>(Func<TFirst, TResult> selector, TResult defaultValue) => first.AsNotNull().Select(selector).DefaultIfEmpty(defaultValue).Max();

        /// <summary>
        ///     <para xml:lang="en">Get maximum value</para>
        ///     <para xml:lang="zh">取最大值</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        public TResult? MaxOrDefault<TResult>(Func<TFirst, TResult> selector) => first.AsNotNull().Select(selector).DefaultIfEmpty().Max();

        /// <summary>
        ///     <para xml:lang="en">Get maximum value</para>
        ///     <para xml:lang="zh">取最大值</para>
        /// </summary>
        public TFirst? MaxOrDefault() => first.AsNotNull().DefaultIfEmpty().Max();

        /// <summary>
        ///     <para xml:lang="en">Get maximum value</para>
        ///     <para xml:lang="zh">取最大值</para>
        /// </summary>
        /// <param name="defaultValue"></param>
        public TFirst? MaxOrDefault(TFirst defaultValue) => first.AsNotNull().DefaultIfEmpty(defaultValue).Max();

        /// <summary>
        ///     <para xml:lang="en">Get minimum value</para>
        ///     <para xml:lang="zh">取最小值</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        public TResult? MinOrDefault<TResult>(Func<TFirst, TResult> selector) => first.AsNotNull().Select(selector).DefaultIfEmpty().Min();

        /// <summary>
        ///     <para xml:lang="en">Get minimum value</para>
        ///     <para xml:lang="zh">取最小值</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="defaultValue"></param>
        public TResult? MinOrDefault<TResult>(Func<TFirst, TResult> selector, TResult defaultValue) => first.AsNotNull().Select(selector).DefaultIfEmpty(defaultValue).Min();

        /// <summary>
        ///     <para xml:lang="en">Get minimum value</para>
        ///     <para xml:lang="zh">取最小值</para>
        /// </summary>
        public TFirst? MinOrDefault() => first.AsNotNull().DefaultIfEmpty().Min();

        /// <summary>
        ///     <para xml:lang="en">Get minimum value</para>
        ///     <para xml:lang="zh">取最小值</para>
        /// </summary>
        /// <param name="defaultValue"></param>
        public TFirst? MinOrDefault(TFirst defaultValue) => first.AsNotNull().DefaultIfEmpty(defaultValue).Min();

        /// <summary>
        ///     <para xml:lang="en">Standard deviation</para>
        ///     <para xml:lang="zh">标准差</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        public TResult? StandardDeviation<TResult>(Func<TFirst, TResult> selector) where TResult : IConvertible
        {
            return first.AsNotNull().Select(t => selector(t).ConvertTo<double>()).StandardDeviation().ConvertTo<TResult>();
        }

        /// <summary>
        ///     <para xml:lang="en">Random order</para>
        ///     <para xml:lang="zh">随机排序</para>
        /// </summary>
        public IOrderedEnumerable<TFirst> OrderByRandom()
        {
            return first.AsNotNull().OrderBy(_ => Guid.NewGuid());
        }

        /// <summary>
        ///     <para xml:lang="en">Sequence equal</para>
        ///     <para xml:lang="zh">序列相等</para>
        /// </summary>
        /// <param name="second"></param>
        /// <param name="condition"></param>
        public bool SequenceEqual(IEnumerable<TFirst> second, Func<TFirst, TFirst, bool> condition)
        {
            if (first is ICollection<TFirst> source1 && second is ICollection<TFirst> source2)
            {
                if (source1.Count != source2.Count)
                {
                    return false;
                }
                if (source1 is IList<TFirst> list1 && source2 is IList<TFirst> list2)
                {
                    var count = source1.Count;
                    for (var index = 0; index < count; ++index)
                    {
                        if (!condition(list1[index], list2[index]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            using var enumerator1 = first.AsNotNull().GetEnumerator();
            using var enumerator2 = second.GetEnumerator();
            while (enumerator1.MoveNext())
            {
                if (!enumerator2.MoveNext() || !condition(enumerator1.Current, enumerator2.Current))
                {
                    return false;
                }
            }
            return !enumerator2.MoveNext();
        }

        /// <summary>
        ///     <para xml:lang="en">Sequence equal</para>
        ///     <para xml:lang="zh">序列相等</para>
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="second"></param>
        /// <param name="condition"></param>
        public bool SequenceEqual<T2>(IEnumerable<T2> second, Func<TFirst, T2, bool> condition)
        {
            if (first is ICollection<TFirst> source1 && second is ICollection<T2> source2)
            {
                if (source1.Count != source2.Count)
                {
                    return false;
                }
                if (source1 is IList<TFirst> list1 && source2 is IList<T2> list2)
                {
                    var count = source1.Count;
                    for (var index = 0; index < count; ++index)
                    {
                        if (!condition(list1[index], list2[index]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            using var enumerator1 = first.AsNotNull().GetEnumerator();
            using var enumerator2 = second.GetEnumerator();
            while (enumerator1.MoveNext())
            {
                if (!enumerator2.MoveNext() || !condition(enumerator1.Current, enumerator2.Current))
                {
                    return false;
                }
            }
            return !enumerator2.MoveNext();
        }

        /// <summary>
        ///     <para xml:lang="en">Execute filter condition if condition is met</para>
        ///     <para xml:lang="zh">满足条件时执行筛选条件</para>
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="where"></param>
        public IEnumerable<TFirst> WhereIf(bool condition, Func<TFirst, bool> where) => condition ? first.AsNotNull().Where(where) : first.AsNotNull();

        /// <summary>
        ///     <para xml:lang="en">Execute filter condition if condition is met</para>
        ///     <para xml:lang="zh">满足条件时执行筛选条件</para>
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="where"></param>
        public IEnumerable<TFirst> WhereIf(Func<bool> condition, Func<TFirst, bool> where) => condition() ? first.AsNotNull().Where(where) : first.AsNotNull();

        /// <summary>
        ///     <para xml:lang="en">Convert collection to SQL IN clause</para>
        ///     <para xml:lang="zh">把集合转成SqlIn</para>
        /// </summary>
        /// <param name="separator">分割符</param>
        /// <param name="left">左边符</param>
        /// <param name="right">右边符</param>
        /// <returns>返回组装好的值，例如"'a','b'"</returns>
        public string ToSqlIn(string separator = ",", string left = "'", string right = "'")
        {
            if (first is null)
            {
                return string.Empty;
            }
            var sb = new StringBuilder();
            foreach (var item in first)
            {
                if (sb.Length > 0)
                {
                    sb.Append(separator);
                }
                sb.Append(left).Append(item).Append(right);
            }
            return sb.ToString();
        }

        /// <summary>
        ///     <para xml:lang="en">Compare changes between two collections</para>
        ///     <para xml:lang="zh">对比两个集合哪些是新增的、删除的、修改的</para>
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="second"></param>
        /// <param name="condition">对比因素条件</param>
        public (List<TFirst> adds, List<T2> remove, List<TFirst> updates) CompareChanges<T2>(IEnumerable<T2>? second, Func<TFirst, T2, bool> condition)
        {
            var firstSource = first.AsNotNull().ToList();
            var secondSource = second ?? [];
            // ReSharper disable once PossibleMultipleEnumeration
            var add = firstSource.Where(f => !secondSource.Any(s => condition(f, s))).ToList();
            // ReSharper disable once PossibleMultipleEnumeration
            var remove = secondSource.Where(s => !firstSource.Any(f => condition(f, s))).ToList();
            // ReSharper disable once PossibleMultipleEnumeration
            var update = firstSource.Where(f => secondSource.Any(s => condition(f, s))).ToList();
            return (add, remove, update);
        }

        /// <summary>
        ///     <para xml:lang="en">Declare collection as non-null</para>
        ///     <para xml:lang="zh">将集合声明为非null集合</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<TFirst> AsNotNull() => first ?? [];
    }

    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    extension<T>(IEnumerable<IEnumerable<T>> source)
    {
        /// <summary>
        ///     <para xml:lang="en">Get intersection of multiple collections</para>
        ///     <para xml:lang="zh">多个集合取交集元素</para>
        /// </summary>
        public IEnumerable<T> IntersectAll()
        {
            return source.Aggregate((current, item) => current.Intersect(item));
        }

        /// <summary>
        ///     <para xml:lang="en">Get intersection of multiple collections</para>
        ///     <para xml:lang="zh">多个集合取交集元素</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        public IEnumerable<T> IntersectAll<TKey>(Func<T, TKey> keySelector)
        {
            return source.Aggregate((current, item) => current.IntersectBy(item.Select(keySelector), keySelector));
        }

        /// <summary>
        ///     <para xml:lang="en">Get intersection of multiple collections</para>
        ///     <para xml:lang="zh">多个集合取交集元素</para>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        /// <param name="comparer"></param>
        public IEnumerable<T> IntersectAll<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return source.Aggregate((current, item) => current.IntersectBy(item.Select(keySelector), keySelector, comparer));
        }

        /// <summary>
        ///     <para xml:lang="en">Get intersection of multiple collections</para>
        ///     <para xml:lang="zh">多个集合取交集元素</para>
        /// </summary>
        /// <param name="comparer"></param>
        public IEnumerable<T> IntersectAll(IEqualityComparer<T> comparer)
        {
            return source.Aggregate((current, item) => current.Intersect(item, comparer));
        }
    }

    /// <param name="this"></param>
    /// <typeparam name="T"></typeparam>
    extension<T>(ICollection<T> @this)
    {
        /// <summary>
        ///     <para xml:lang="en">Add multiple elements</para>
        ///     <para xml:lang="zh">添加多个元素</para>
        /// </summary>
        /// <param name="values"></param>
        public void AddRange(params T[] values)
        {
            foreach (var obj in values)
            {
                @this.Add(obj);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Add multiple elements</para>
        ///     <para xml:lang="zh">添加多个元素</para>
        /// </summary>
        /// <param name="values"></param>
        public void AddRange(IEnumerable<T> values)
        {
            foreach (var obj in values)
            {
                @this.Add(obj);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Add multiple elements that meet the condition</para>
        ///     <para xml:lang="zh">添加符合条件的多个元素</para>
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="values"></param>
        public void AddRangeIf(Func<T, bool> predicate, params T[] values)
        {
            foreach (var obj in values)
            {
                if (predicate(obj))
                {
                    @this.Add(obj);
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Add non-duplicate elements</para>
        ///     <para xml:lang="zh">添加不重复的元素</para>
        /// </summary>
        /// <param name="values"></param>
        public void AddRangeIfNotContains(params T[] values)
        {
            foreach (var obj in values)
            {
                if (!@this.Contains(obj))
                {
                    @this.Add(obj);
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Remove elements that meet the condition</para>
        ///     <para xml:lang="zh">移除符合条件的元素</para>
        /// </summary>
        /// <param name="where"></param>
        public void RemoveWhere(Func<T, bool> where)
        {
            foreach (var obj in @this.Where(where).ToList())
            {
                @this.Remove(obj);
            }
        }
    }

    /// <param name="this"></param>
    /// <typeparam name="T"></typeparam>
    extension<T>(ConcurrentBag<T> @this)
    {
        /// <summary>
        ///     <para xml:lang="en">Add multiple elements</para>
        ///     <para xml:lang="zh">添加多个元素</para>
        /// </summary>
        /// <param name="values"></param>
        public void AddRange(params T[] values)
        {
            foreach (var obj in values)
            {
                @this.Add(obj);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Add multiple elements that meet the condition</para>
        ///     <para xml:lang="zh">添加符合条件的多个元素</para>
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="values"></param>
        public void AddRangeIf(Func<T, bool> predicate, params T[] values)
        {
            foreach (var obj in values)
            {
                if (predicate(obj))
                {
                    @this.Add(obj);
                }
            }
        }
    }

    /// <param name="this"></param>
    /// <typeparam name="T"></typeparam>
    extension<T>(ConcurrentQueue<T> @this)
    {
        /// <summary>
        ///     <para xml:lang="en">Add multiple elements</para>
        ///     <para xml:lang="zh">添加多个元素</para>
        /// </summary>
        /// <param name="values"></param>
        public void AddRange(params T[] values)
        {
            foreach (var obj in values)
            {
                @this.Enqueue(obj);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Add multiple elements that meet the condition</para>
        ///     <para xml:lang="zh">添加符合条件的多个元素</para>
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="values"></param>
        public void AddRangeIf(Func<T, bool> predicate, params T[] values)
        {
            foreach (var obj in values)
            {
                if (predicate(obj))
                {
                    @this.Enqueue(obj);
                }
            }
        }
    }

    /// <param name="list"></param>
    /// <typeparam name="T"></typeparam>
    extension<T>(IList<T> list)
    {
        /// <summary>
        ///     <para xml:lang="en">Insert element after specified element</para>
        ///     <para xml:lang="zh">在元素之后添加元素</para>
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="value">值</param>
        public void InsertAfter(Func<T, bool> condition, T value)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (condition(list[i]))
                {
                    list.Insert(i + 1, value);
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Insert element after specified index</para>
        ///     <para xml:lang="zh">在元素之后添加元素</para>
        /// </summary>
        /// <param name="index">索引位置</param>
        /// <param name="value">值</param>
        public void InsertAfter(int index, T value)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (i == index)
                {
                    list.Insert(i + 1, value);
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Change the index position of an element</para>
        ///     <para xml:lang="zh">改变元素的索引位置</para>
        /// </summary>
        /// <param name="item">元素</param>
        /// <param name="index">索引值</param>
        /// <exception cref="ArgumentNullException"></exception>
        public IList<T> ChangeIndex(T item, int index)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            ChangeIndexInternal(list, item, index);
            return list;
        }

        /// <summary>
        ///     <para xml:lang="en">Change the index position of an element</para>
        ///     <para xml:lang="zh">改变元素的索引位置</para>
        /// </summary>
        /// <param name="condition">元素定位条件</param>
        /// <param name="index">索引值</param>
        public IList<T> ChangeIndex(Func<T, bool> condition, int index)
        {
            var item = list.FirstOrDefault(condition);
            if (item != null)
            {
                ChangeIndexInternal(list, item, index);
            }
            return list;
        }
    }

    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    extension<TSource>(IQueryable<TSource> source)
    {
        /// <summary>
        ///     <para xml:lang="en">Get maximum value</para>
        ///     <para xml:lang="zh">取最大值</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        public TResult? MaxOrDefault<TResult>(Expression<Func<TSource, TResult>> selector) => source.Select(selector).DefaultIfEmpty().Max();

        /// <summary>
        ///     <para xml:lang="en">Get maximum value</para>
        ///     <para xml:lang="zh">取最大值</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="defaultValue"></param>
        public TResult? MaxOrDefault<TResult>(Expression<Func<TSource, TResult>> selector, TResult defaultValue) => source.Select(selector).DefaultIfEmpty(defaultValue).Max();

        /// <summary>
        ///     <para xml:lang="en">Get maximum value</para>
        ///     <para xml:lang="zh">取最大值</para>
        /// </summary>
        public TSource? MaxOrDefault() => source.DefaultIfEmpty().Max();

        /// <summary>
        ///     <para xml:lang="en">Get maximum value</para>
        ///     <para xml:lang="zh">取最大值</para>
        /// </summary>
        /// <param name="defaultValue"></param>
        public TSource? MaxOrDefault(TSource defaultValue) => source.DefaultIfEmpty(defaultValue).Max();

        /// <summary>
        ///     <para xml:lang="en">Get minimum value</para>
        ///     <para xml:lang="zh">取最小值</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        public TResult? MinOrDefault<TResult>(Expression<Func<TSource, TResult>> selector) => source.Select(selector).DefaultIfEmpty().Min();

        /// <summary>
        ///     <para xml:lang="en">Get minimum value</para>
        ///     <para xml:lang="zh">取最小值</para>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <param name="defaultValue"></param>
        public TResult? MinOrDefault<TResult>(Expression<Func<TSource, TResult>> selector, TResult defaultValue) => source.Select(selector).DefaultIfEmpty(defaultValue).Min();

        /// <summary>
        ///     <para xml:lang="en">Get minimum value</para>
        ///     <para xml:lang="zh">取最小值</para>
        /// </summary>
        public TSource? MinOrDefault() => source.DefaultIfEmpty().Min();

        /// <summary>
        ///     <para xml:lang="en">Get minimum value</para>
        ///     <para xml:lang="zh">取最小值</para>
        /// </summary>
        /// <param name="defaultValue"></param>
        public TSource? MinOrDefault(TSource defaultValue) => source.DefaultIfEmpty(defaultValue).Min();

        /// <summary>
        ///     <para xml:lang="en">Execute filter condition if condition is met</para>
        ///     <para xml:lang="zh">满足条件时执行筛选条件</para>
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="where"></param>
        public IQueryable<TSource> WhereIf(bool condition, Expression<Func<TSource, bool>> where) => condition ? source.Where(where) : source;

        /// <summary>
        ///     <para xml:lang="en">Execute filter condition if condition is met</para>
        ///     <para xml:lang="zh">满足条件时执行筛选条件</para>
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="where"></param>
        public IQueryable<TSource> WhereIf(Func<bool> condition, Expression<Func<TSource, bool>> where) => condition() ? source.Where(where) : source;
    }
}
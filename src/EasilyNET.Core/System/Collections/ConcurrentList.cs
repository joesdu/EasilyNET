using System.Collections;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.System.Collections;

/// <summary>
///     <para xml:lang="en">Thread-safe List collection</para>
///     <para xml:lang="zh">线程安全的 List 集合</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The type of elements in the list</para>
///     <para xml:lang="zh">列表中元素的类型</para>
/// </typeparam>
public sealed class ConcurrentList<T> : IList<T>
{
    private readonly List<T> _list = [];
    private readonly Lock _syncRoot = new();

    /// <inheritdoc />
    public T this[int index]
    {
        get
        {
            lock (_syncRoot)
            {
                return _list[index];
            }
        }
        set
        {
            lock (_syncRoot)
            {
                _list[index] = value;
            }
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _list.Count;
            }
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(T item)
    {
        lock (_syncRoot)
        {
            _list.Add(item);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_syncRoot)
        {
            _list.Clear();
        }
    }

    /// <inheritdoc />
    public bool Contains(T item)
    {
        lock (_syncRoot)
        {
            return _list.Contains(item);
        }
    }

    /// <inheritdoc />
    public int IndexOf(T item)
    {
        lock (_syncRoot)
        {
            return _list.IndexOf(item);
        }
    }

    /// <inheritdoc />
    public void Insert(int index, T item)
    {
        lock (_syncRoot)
        {
            _list.Insert(index, item);
        }
    }

    /// <inheritdoc />
    public bool Remove(T item)
    {
        lock (_syncRoot)
        {
            return _list.Remove(item);
        }
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        lock (_syncRoot)
        {
            _list.RemoveAt(index);
        }
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        lock (_syncRoot)
        {
            return new List<T>(_list).GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_syncRoot)
        {
            _list.CopyTo(array, arrayIndex);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Finds all elements that match the conditions defined by the specified predicate</para>
    ///     <para xml:lang="zh">查找与指定谓词定义的条件匹配的所有元素</para>
    /// </summary>
    /// <param name="match">
    ///     <para xml:lang="en">The predicate delegate that defines the conditions of the elements to search for</para>
    ///     <para xml:lang="zh">定义要搜索的元素条件的谓词委托</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A List containing all the elements that match the conditions defined by the specified predicate</para>
    ///     <para xml:lang="zh">包含与指定谓词定义的条件匹配的所有元素的列表</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the match predicate is null</para>
    ///     <para xml:lang="zh">当匹配谓词为空时抛出</para>
    /// </exception>
    public List<T> FindAll(Predicate<T> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        lock (_syncRoot)
        {
            return _list.FindAll(match);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Adds the elements of the specified collection to the end of the list</para>
    ///     <para xml:lang="zh">将指定集合的元素添加到列表的末尾</para>
    /// </summary>
    /// <param name="items">
    ///     <para xml:lang="en">The collection whose elements should be added to the end of the list</para>
    ///     <para xml:lang="zh">其元素应添加到列表末尾的集合</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the items collection is null</para>
    ///     <para xml:lang="zh">当项目集合为空时抛出</para>
    /// </exception>
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        lock (_syncRoot)
        {
            _list.AddRange(items);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Removes all the elements that match the conditions defined by the specified predicate</para>
    ///     <para xml:lang="zh">删除与指定谓词定义的条件匹配的所有元素</para>
    /// </summary>
    /// <param name="match">
    ///     <para xml:lang="en">The predicate delegate that defines the conditions of the elements to remove</para>
    ///     <para xml:lang="zh">定义要删除的元素条件的谓词委托</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The number of elements removed from the list</para>
    ///     <para xml:lang="zh">从列表中删除的元素数量</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the match predicate is null</para>
    ///     <para xml:lang="zh">当匹配谓词为空时抛出</para>
    /// </exception>
    public int RemoveAll(Predicate<T> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        lock (_syncRoot)
        {
            return _list.RemoveAll(match);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the list to a List</para>
    ///     <para xml:lang="zh">将列表转换为 List</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A List containing all the elements in the list</para>
    ///     <para xml:lang="zh">包含列表中所有元素的 List</para>
    /// </returns>
    public List<T> ToList()
    {
        lock (_syncRoot)
        {
            return [.. _list];
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the list to an array</para>
    ///     <para xml:lang="zh">将列表转换为数组</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">An array containing all the elements in the list</para>
    ///     <para xml:lang="zh">包含列表中所有元素的数组</para>
    /// </returns>
    public T[] ToArray()
    {
        lock (_syncRoot)
        {
            return _list.ToArray();
        }
    }
}
namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Array extensions</para>
///     <para xml:lang="zh">数组扩展</para>
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Iterates over each element in the array and performs the specified action</para>
    ///     <para xml:lang="zh">遍历数组中的每个元素并执行指定的操作</para>
    /// </summary>
    /// <param name="array">
    ///     <para xml:lang="en">The array to iterate over</para>
    ///     <para xml:lang="zh">要遍历的数组</para>
    /// </param>
    /// <param name="action">
    ///     <para xml:lang="en">The action to perform on each element</para>
    ///     <para xml:lang="zh">对每个元素执行的操作</para>
    /// </param>
    public static void ForEach(this Array array, Action<Array, int[]> action)
    {
        if (array.LongLength == 0) return;
        var walker = new ArrayTraverse(array);
        do
        {
            action(array, walker.Position);
        } while (walker.Step());
    }
}

/// <summary>
///     <para xml:lang="en">Helper class to traverse an array</para>
///     <para xml:lang="zh">帮助类，用于遍历数组</para>
/// </summary>
file sealed class ArrayTraverse
{
    private readonly int[] _maxLengths;
    public readonly int[] Position;

    /// <summary>
    ///     <para xml:lang="en">Constructor, initializes the array traverse helper</para>
    ///     <para xml:lang="zh">构造函数，初始化数组遍历帮助类</para>
    /// </summary>
    /// <param name="array">
    ///     <para xml:lang="en">The array to traverse</para>
    ///     <para xml:lang="zh">要遍历的数组</para>
    /// </param>
    public ArrayTraverse(Array array)
    {
        var rank = array.Rank;
        _maxLengths = new int[rank];
        Position = new int[rank];
        for (var i = 0; i < rank; ++i)
        {
            _maxLengths[i] = array.GetLength(i) - 1;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Moves to the next element in the array</para>
    ///     <para xml:lang="zh">移动到数组中的下一个元素</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">Returns <see langword="true" /> if successfully moved to the next element; otherwise, <see langword="false" /></para>
    ///     <para xml:lang="zh">如果成功移动到下一个元素，则返回 <see langword="true" />；否则返回 <see langword="false" /></para>
    /// </returns>
    public bool Step()
    {
        for (var i = 0; i < Position.Length; ++i)
        {
            if (Position[i] >= _maxLengths[i]) continue;
            Position[i]++;
            for (var j = 0; j < i; j++)
            {
                Position[j] = 0;
            }
            return true;
        }
        return false;
    }
}
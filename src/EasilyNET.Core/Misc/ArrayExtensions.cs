namespace EasilyNET.Core.Misc;

/// <summary>
/// ArrayExtensions
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// ForEach
    /// </summary>
    /// <param name="array">要遍历的数组</param>
    /// <param name="action">对每个元素执行的操作</param>
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

file sealed class ArrayTraverse
{
    private readonly int[] _maxLengths;
    public readonly int[] Position;

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
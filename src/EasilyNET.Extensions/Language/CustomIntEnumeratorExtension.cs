// ReSharper disable UnusedMember.Global

// ReSharper disable UnusedType.Global

namespace EasilyNET.Extensions.Language;

/*
 * example:
 * foreach (var i in 3..9)
 * {
 *     Console.Write(i);
 * }
 * Out: 3456789
 */
/// <summary>
/// CustomIntEnumeratorExtension
/// </summary>
public static class CustomIntEnumeratorExtension
{
    /// <summary>
    /// GetEnumerator
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    public static CustomIntEnumerator GetEnumerator(this Range range) => new(range);

    /// <summary>
    /// GetEnumerator
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static CustomIntEnumerator GetEnumerator(this int number) => new(new(0, number));

    /// <summary>
    /// 自定义枚举器
    /// </summary>
    // public ref struct CustomIntEnumerator ,这个ref是否有必要?
    public struct CustomIntEnumerator
    {
        private readonly int _end;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="range"></param>
        public CustomIntEnumerator(Range range)
        {
            if (range.End.IsFromEnd) throw new NotSupportedException();
            Current = range.Start.Value - 1;
            _end = range.End.Value;
        }

        /// <summary>
        /// 当前索引
        /// </summary>
        public int Current { get; private set; }

        /// <summary>
        /// 移动到下一个
        /// </summary>
        public bool MoveNext() => Current++ < _end;
    }
}
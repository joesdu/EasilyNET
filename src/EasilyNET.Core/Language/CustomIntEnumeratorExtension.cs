// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Language;

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
    /// <example>
    ///     <code>
    /// <![CDATA[
    /// foreach (var i in ..3)
    /// {
    ///     Console.WriteLine(i);
    /// }
    /// OutPut:
    /// 0,1,2,3
    /// foreach (var i in 1..3)
    /// {
    ///     Console.WriteLine(i);
    /// }
    /// OutPut:
    /// 1,2,3
    /// foreach (var i in 3)
    /// {
    ///     Console.WriteLine(i);
    /// }
    /// OutPut:
    /// 0,1,2,3
    ///   ]]>
    ///  </code>
    /// </example>
    public struct CustomIntEnumerator
    {
        // public ref struct CustomIntEnumerator
        // ref struct的设计是为了限制该结构体只能堆栈上分配,
        // 不能在托管堆上分配.这通常用于高性能的场景,以减少垃圾回收的压力.
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
        // ReSharper disable once MemberCanBePrivate.Global
        public int Current { get; set; }

        /// <summary>
        /// 移动到下一个
        /// </summary>
        public bool MoveNext() => Current++ < _end;
    }
}
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Language;

/// <summary>
/// 提供自定义整数枚举器的扩展方法。
/// </summary>
public static class CustomIntEnumeratorExtension
{
    /// <summary>
    /// 为 <see cref="Range" /> 提供自定义枚举器。
    /// </summary>
    /// <param name="range">要枚举的范围。</param>
    /// <returns>自定义整数枚举器。</returns>
    public static CustomIntEnumerator GetEnumerator(this Range range) => new(range);

    /// <summary>
    /// 为整数提供自定义枚举器，从 0 到指定的整数。
    /// </summary>
    /// <param name="number">要枚举的最大整数。</param>
    /// <returns>自定义整数枚举器。</returns>
    public static CustomIntEnumerator GetEnumerator(this int number) => new(new(0, number));

    /// <summary>
    /// 自定义整数枚举器。
    /// <example>
    ///     <code>
    /// <![CDATA[
    /// foreach (var i in ..3)
    /// {
    ///     Console.WriteLine(i);
    /// }
    /// Output: 0, 1, 2, 3
    /// ---------------------------
    /// foreach (var i in 1..3)
    /// {
    ///     Console.WriteLine(i);
    /// }
    /// Output: 1, 2, 3
    /// ---------------------------
    /// foreach (var i in 3)
    /// {
    ///     Console.WriteLine(i);
    /// }
    /// Output: 0, 1, 2, 3
    /// ]]>
    /// </code>
    /// </example>
    /// </summary>
    public struct CustomIntEnumerator
    {
        // public ref struct CustomIntEnumerator
        // ref struct的设计是为了限制该结构体只能堆栈上分配,
        // 不能在托管堆上分配.这通常用于高性能的场景,以减少垃圾回收的压力.

        private readonly int _end;

        /// <summary>
        /// 构造函数，初始化自定义整数枚举器。
        /// </summary>
        /// <param name="range">要枚举的范围。</param>
        /// <exception cref="NotSupportedException">如果范围的结束值是从末尾计算的，则抛出此异常。</exception>
        public CustomIntEnumerator(Range range)
        {
            if (range.End.IsFromEnd) throw new NotSupportedException("不支持从末尾计算的范围。");
            Current = range.Start.Value - 1;
            _end = range.End.Value;
        }

        /// <summary>
        /// 当前索引。
        /// </summary>
        public int Current { get; private set; }

        /// <summary>
        /// 移动到下一个元素。
        /// </summary>
        /// <returns>如果成功移动到下一个元素，则返回 <see langword="true" />；否则返回 <see langword="false" />。</returns>
        public bool MoveNext() => Current++ < _end;
    }
}
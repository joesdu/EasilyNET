// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Language;

/// <summary>
///     <para xml:lang="en">Extension methods for custom integer enumerator</para>
///     <para xml:lang="zh">自定义整数枚举器的扩展方法</para>
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
    ///     <para xml:lang="en">Gets a custom integer enumerator for the specified number</para>
    ///     <para xml:lang="zh">获取指定数字的自定义整数枚举器</para>
    /// </summary>
    /// <param name="number">
    ///     <para xml:lang="en">The number to enumerate</para>
    ///     <para xml:lang="zh">要枚举的数字</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A custom integer enumerator</para>
    ///     <para xml:lang="zh">自定义整数枚举器</para>
    /// </returns>
    public static CustomIntEnumerator GetEnumerator(this int number) => new(new(0, number));

    /// <summary>
    ///     <para xml:lang="en">Custom integer enumerator</para>
    ///     <para xml:lang="zh">自定义整数枚举器</para>
    ///     <example>
    ///         <code>
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
    ///         </code>
    ///     </example>
    /// </summary>
    public struct CustomIntEnumerator
    {
        // public ref struct CustomIntEnumerator
        // ref struct的设计是为了限制该结构体只能栈上分配,
        // 不能在托管堆上分配.这通常用于高性能的场景,以减少垃圾回收的压力.

        private readonly int _end;

        /// <summary>
        ///     <para xml:lang="en">Constructor, initializes the custom integer enumerator</para>
        ///     <para xml:lang="zh">构造函数，初始化自定义整数枚举器</para>
        /// </summary>
        /// <param name="range">
        ///     <para xml:lang="en">The range to enumerate</para>
        ///     <para xml:lang="zh">要枚举的范围</para>
        /// </param>
        /// <exception cref="NotSupportedException">
        ///     <para xml:lang="en">Thrown if the end value of the range is calculated from the end</para>
        ///     <para xml:lang="zh">如果范围的结束值是从末尾计算的，则抛出此异常</para>
        /// </exception>
        public CustomIntEnumerator(Range range)
        {
            if (range.End.IsFromEnd) throw new NotSupportedException("不支持从末尾计算的范围。");
            Current = range.Start.Value - 1;
            _end = range.End.Value;
        }

        /// <summary>
        ///     <para xml:lang="en">Current index</para>
        ///     <para xml:lang="zh">当前索引</para>
        /// </summary>
        public int Current { get; private set; }

        /// <summary>
        ///     <para xml:lang="en">Move to the next element</para>
        ///     <para xml:lang="zh">移动到下一个元素</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">Returns <see langword="true" /> if successfully moved to the next element; otherwise, <see langword="false" /></para>
        ///     <para xml:lang="zh">如果成功移动到下一个元素，则返回 <see langword="true" />；否则返回 <see langword="false" /></para>
        /// </returns>
        public bool MoveNext() => Current++ < _end;
    }
}
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.Core;

/// <summary>
///     <para xml:lang="en">Type used for Unwind operator</para>
///     <para xml:lang="zh">Unwind 操作符使用的类型</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">Generic type parameter</para>
///     <para xml:lang="zh">泛型类型参数</para>
/// </typeparam>
public class UnwindObj<T>
{
    /// <summary>
    ///     <para xml:lang="en">1. T as List, use for Projection, 2. T as single Object, use for MongoDB array field Unwind result</para>
    ///     <para xml:lang="zh">1. T 作为列表，用于投影，2. T 作为单个对象，用于 MongoDB 数组字段 Unwind 结果</para>
    /// </summary>
    public T? Obj { get; set; }

    /// <summary>
    ///     <para xml:lang="en">When T as List, record Count</para>
    ///     <para xml:lang="zh">当 T 作为列表时，记录数量</para>
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Record array field element's index before Unwinds</para>
    ///     <para xml:lang="zh">记录 Unwind 之前数组字段元素的索引</para>
    /// </summary>
    public int Index { get; set; }
}
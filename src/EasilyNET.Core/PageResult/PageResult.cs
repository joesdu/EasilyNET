// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core;

/// <summary>
///     <para xml:lang="en">Pagination data return</para>
///     <para xml:lang="zh">分页数据返回</para>
/// </summary>
public static class PageResult
{
    /// <summary>
    ///     <para xml:lang="en">Generic type pagination data return</para>
    ///     <para xml:lang="zh">泛型类型分页数据返回</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The data type</para>
    ///     <para xml:lang="zh">数据类型</para>
    /// </typeparam>
    /// <param name="total">
    ///     <para xml:lang="en">The total number of data items</para>
    ///     <para xml:lang="zh">数据总量</para>
    /// </param>
    /// <param name="list">
    ///     <para xml:lang="en">The paginated data</para>
    ///     <para xml:lang="zh">分页数据</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A PageResult object containing the total number of data items and the paginated data</para>
    ///     <para xml:lang="zh">包含数据总量和分页数据的 PageResult 对象</para>
    /// </returns>
    public static PageResult<T> Wrap<T>(long? total, IEnumerable<T>? list) => new(total, list);

    /// <summary>
    ///     <para xml:lang="en">Dynamic type pagination data return</para>
    ///     <para xml:lang="zh">动态类型分页数据返回</para>
    /// </summary>
    /// <param name="total">
    ///     <para xml:lang="en">The total number of data items</para>
    ///     <para xml:lang="zh">数据总量</para>
    /// </param>
    /// <param name="list">
    ///     <para xml:lang="en">The paginated data</para>
    ///     <para xml:lang="zh">分页数据</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A PageResult object containing the total number of data items and the paginated data</para>
    ///     <para xml:lang="zh">包含数据总量和分页数据的 PageResult 对象</para>
    /// </returns>
    public static PageResult<dynamic> WrapDynamic(long? total, IEnumerable<dynamic>? list) => new(total, list);
}

/// <summary>
///     <para xml:lang="en">Pagination data return</para>
///     <para xml:lang="zh">分页数据返回</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The data type</para>
///     <para xml:lang="zh">数据类型</para>
/// </typeparam>
/// <param name="total">
///     <para xml:lang="en">The total number of data items</para>
///     <para xml:lang="zh">数据总量</para>
/// </param>
/// <param name="list">
///     <para xml:lang="en">The paginated data</para>
///     <para xml:lang="zh">分页数据</para>
/// </param>
public sealed class PageResult<T>(long? total, IEnumerable<T>? list)
{
    /// <summary>
    ///     <para xml:lang="en">The total number of data items</para>
    ///     <para xml:lang="zh">数据量总数</para>
    /// </summary>
    public long Total { get; } = total ?? 0;

    /// <summary>
    ///     <para xml:lang="en">The list of data items</para>
    ///     <para xml:lang="zh">数据列表</para>
    /// </summary>
    public IEnumerable<T>? List { get; } = list;
}
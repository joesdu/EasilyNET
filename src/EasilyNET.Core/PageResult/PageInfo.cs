// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core;

/// <summary>
///     <para xml:lang="en">Pagination information</para>
///     <para xml:lang="zh">分页信息</para>
/// </summary>
public class PageInfo
{
    /// <summary>
    ///     <para xml:lang="en">Page number</para>
    ///     <para xml:lang="zh">页码</para>
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Number of items per page</para>
    ///     <para xml:lang="zh">每页数据量</para>
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Number of items to skip</para>
    ///     <para xml:lang="zh">跳过的数据量</para>
    /// </summary>
    public int Skip => (Current - 1) * Size;
}

/// <summary>
///     <para xml:lang="en">Pagination with keyword search</para>
///     <para xml:lang="zh">关键字查询分页</para>
/// </summary>
public class KeywordPageInfo : PageInfo
{
    /// <summary>
    ///     <para xml:lang="en">Search keyword</para>
    ///     <para xml:lang="zh">搜索关键字</para>
    /// </summary>
    public string? Key { get; set; }
}

/// <summary>
///     <para xml:lang="en">Pagination with keyword search and data status</para>
///     <para xml:lang="zh">根据数据状态查询</para>
/// </summary>
public class KeywordIsEnablePageInfo : KeywordPageInfo
{
    /// <summary>
    ///     <para xml:lang="en">Data status, null means all, true means enabled, false means disabled</para>
    ///     <para xml:lang="zh">数据状态，null 表示所有，true 表示正常，false 表示禁用</para>
    /// </summary>
    public bool? Enable { get; set; }
}
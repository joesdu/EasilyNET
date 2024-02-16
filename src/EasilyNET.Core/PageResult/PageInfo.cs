// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core;

/// <summary>
/// 分页信息
/// </summary>
public class PageInfo
{
    /// <summary>
    /// 页码
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    /// 每页数据量
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// 跳过的数据量
    /// </summary>
    public int Skip => (Current - 1) * Size;
}

/// <summary>
/// 关键字查询分页
/// </summary>
public class KeywordPageInfo : PageInfo
{
    /// <summary>
    /// 搜索关键字
    /// </summary>
    public string? Key { get; set; }
}

/// <summary>
/// 根据数据状态查询
/// </summary>
public class KeywordIsEnablePageInfo : KeywordPageInfo
{
    /// <summary>
    /// 数据状态,null 表示所有,true 正常,false 禁用
    /// </summary>
    public bool? Enable { get; set; }
}
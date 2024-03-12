// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

using EasilyNET.Core;

namespace WebApi.Test.Unit;

/// <summary>
/// 文件信息查询实体
/// </summary>
public class InfoSearch : KeywordPageInfo
{
    /// <summary>
    /// 文件名称
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// APP名称[通常指业务系统的系统名称]
    /// </summary>
    public string App { get; set; } = string.Empty;

    /// <summary>
    /// 业务名称[通常指业务系统中的某个业务,如订单业务等]
    /// </summary>
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 查询时间范围开始时间
    /// </summary>
    public DateTime? Start { get; set; }

    /// <summary>
    /// 查询时间范围结束时间
    /// </summary>
    public DateTime? End { get; set; }
}

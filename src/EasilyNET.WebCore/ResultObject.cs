using System.Net;

namespace EasilyNET.WebCore;

/// <summary>
/// 返回对象实体
/// </summary>
public class ResultObject
{
    /// <summary>
    /// 状态码
    /// </summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    /// <summary>
    /// 数据信息
    /// </summary>
    public string? Msg { get; set; } = string.Empty;

    /// <summary>
    /// 数据实体
    /// </summary>
    public object? Data { get; set; }
}
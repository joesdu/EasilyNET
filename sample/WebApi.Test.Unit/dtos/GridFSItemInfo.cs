namespace WebApi.Test.Unit;

/// <summary>
/// 用来记录文件信息的实体.
/// </summary>
public class GridFSItemInfo
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 文件名称
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 长度
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// 文件ContentType
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID[通常是业务系统中的用户ID]
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 用户名称[通常是业务系统中的用户名]
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// APP名称[通常指业务系统的系统名称]
    /// </summary>
    public string App { get; set; } = string.Empty;

    /// <summary>
    /// 业务名称[通常指业务系统中的某个业务,如订单业务等]
    /// </summary>
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 上级Id[当文件存在目录结构的时候,可以使用该ID来表示上一级的目录Id]
    /// </summary>
    public string? CategoryId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
}

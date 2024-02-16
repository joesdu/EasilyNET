namespace WebApi.Test.Unit;

/// <summary>
/// 文件信息的实体
/// </summary>
public class GridFSItem
{
    /// <summary>
    /// 文件Id
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// 文件名称
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件长度
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// ContentType
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace WebApi.Test.Unit;

/// <summary>
/// 上传文件的基本信息
/// </summary>
public class UploadGridFSInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 用户名称
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// BusinessApp与该值至少要有一个.当该值存在时,优先取此值.
    /// </summary>
    public string App { get; set; } = string.Empty;

    /// <summary>
    /// Business类型
    /// </summary>
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 可用于资源中心,记录所属目录id
    /// </summary>
    public string? CategoryId { get; set; }
}

/// <summary>
/// 多文件上传的扩展信息
/// </summary>
public class UploadGridFSMulti : UploadGridFSInfo
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public List<string> DeleteIds { get; set; } = [];

    /// <summary>
    /// 上传文件(单或多文件)
    /// </summary>
    [Required]
    public IFormFileCollection? File { get; set; }
}

/// <summary>
/// 单文件上传的扩展信息
/// </summary>
public class UploadGridFSSingle : UploadGridFSInfo
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public string? DeleteId { get; set; }

    /// <summary>
    /// 上传文件(单或多文件)
    /// </summary>
    [Required]
    public IFormFile? File { get; set; }
}

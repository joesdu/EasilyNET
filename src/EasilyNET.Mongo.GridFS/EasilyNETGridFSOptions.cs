using MongoDB.Driver.GridFS;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.Mongo.GridFS;

/// <summary>
/// 服务的一些参数
/// </summary>
public class EasilyNETGridFSOptions
{
    /// <summary>
    /// GridFSBucketOptions
    /// </summary>
    public GridFSBucketOptions? Options { get; set; } = null;

    /// <summary>
    /// APP名称[通常指业务系统的系统名称]
    /// </summary>
    public string BusinessApp { get; set; } = string.Empty;

    /// <summary>
    /// 默认数据库
    /// </summary>
    public bool DefaultDB { get; set; } = true;

    /// <summary>
    /// 文件信息表名称,默认为[item.info]
    /// </summary>
    public string ItemInfo { get; set; } = "item.info";
}
namespace EasilyNET.Mongo.GridFS.Extension;

/// <summary>
/// 虚拟文件路径的一些参数配置
/// </summary>
public class EasilyFSSettings
{
    /// <summary>
    /// 配置节点名称
    /// </summary>
    public const string Position = "EasilyFS";

    /// <summary>
    /// 虚拟路径,默认: /easilyfs
    /// </summary>
    public string VirtualPath { get; set; } = "/easilyfs";

    /// <summary>
    /// 物理路径,实际存储文件的位置
    /// </summary>
    public string PhysicalPath { get; set; } = string.Empty;
}
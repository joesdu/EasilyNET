namespace EasilyNET.Core.Domains;

/// <summary>
/// 是否版本号
/// </summary>
public interface IHasRowVersion
{
    /// <summary>
    /// 版本号
    /// </summary>
    byte[] Version { get; set; } 
}
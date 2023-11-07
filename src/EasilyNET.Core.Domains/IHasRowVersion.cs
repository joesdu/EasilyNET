namespace EasilyNET.Core.Domains;

/// <summary>
/// 并发控制版本号
/// </summary>
public interface IHasRowVersion
{
    /// <summary>
    /// 版本号
    /// </summary>
    byte[] Version { get; set; }
}
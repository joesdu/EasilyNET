namespace EasilyNET.Core.Domains;

/// <summary>
/// 修改时间
/// </summary>
public interface IHasModificationTime 
{
    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime? LastModificationTime { get; }
}
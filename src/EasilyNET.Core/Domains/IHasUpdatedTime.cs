namespace EasilyNET.Core.Domains;

/// <summary>
/// 更新时间
/// </summary>
public interface IHasUpdatedTime
{
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; }
}
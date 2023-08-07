namespace EasilyNET.Core.Entities;

/// <summary>
/// 更新时间
/// </summary>
public interface IHasUpdatedTime
{
    /// <summary>
    /// </summary>
    public DateTime? UpdatedTime { get; }
}
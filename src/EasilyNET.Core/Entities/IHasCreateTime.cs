namespace EasilyNET.Core.Entities;

/// <summary>
/// 是否创建时间
/// </summary>
public interface IHasCreateTime
{
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreateTime { get; }
}
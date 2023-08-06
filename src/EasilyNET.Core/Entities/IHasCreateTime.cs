namespace EasilyNET.Core.Entities;

/// <summary>
/// 是否创建时间
/// </summary>
public interface IHasCreateTime
{

    public DateTime? CreateTime { get; }
}
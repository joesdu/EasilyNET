namespace EasilyNET.Core.Domains;

/// <summary>
/// 是否删除时间
/// </summary>
public interface IHasDeletionTime:IHasSoftDelete
{
    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DeletionTime { get; }
}
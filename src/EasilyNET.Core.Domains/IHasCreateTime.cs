namespace EasilyNET.Core.Domains;

/// <summary>
/// 是否创建时间
/// </summary>
public interface IHasCreationTime
{
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }
}
namespace EasilyNET.Core.Domains;

/// <summary>
/// 是否删除者ID
/// </summary>
/// <typeparam name="TDeleterId">删除者ID</typeparam>
public interface IHasDeleterId<TDeleterId>
{
    /// <summary>
    /// 最后删除者ID
    /// </summary>
    TDeleterId? DeleterId { get; set; }
}
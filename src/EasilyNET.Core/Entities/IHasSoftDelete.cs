namespace EasilyNET.Core.Entities;

/// <summary>
/// 是否软删除
/// </summary>
public interface IHasSoftDelete
{
    /// <summary>
    /// 是否删除
    /// </summary>
    bool IsDelete { get; }
}
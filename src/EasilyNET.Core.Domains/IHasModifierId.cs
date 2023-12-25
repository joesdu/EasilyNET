namespace EasilyNET.Core.Domains;

/// <summary>
/// 修改者ID
/// </summary>
public interface IHasModifierId<ModifierId>
{
    /// <summary>
    /// 最后修改者ID
    /// </summary>
    ModifierId? LastModifierId { get; set; }
}
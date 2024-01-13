namespace EasilyNET.Core.Domains;

/// <summary>
/// 修改者ID
/// </summary>
/// <typeparam name="ModifierId"></typeparam>
public interface IHasModifierId<ModifierId>
{
    /// <summary>
    /// 最后修改者ID
    /// </summary>
    ModifierId? LastModifierId { get; }
}
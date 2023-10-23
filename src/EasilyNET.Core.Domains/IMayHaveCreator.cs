namespace EasilyNET.Core.Domains;

/// <summary>
/// 是否可空创建者ID
/// </summary>
/// <typeparam name="TCreatorId"></typeparam>
public interface IMayHaveCreator<TCreatorId>
{
    /// <summary>
    /// 创建者ID
    /// </summary>
    TCreatorId? CreatorId { get; set; }
}
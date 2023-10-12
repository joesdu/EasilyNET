namespace EasilyNET.EntityFrameworkCore;

/// <summary>
/// EF CORE共享
/// </summary>
public static class EFCoreShare
{
    /// <summary>
    /// 是否删除
    /// </summary>
    public const string IsDeleted = nameof(IsDeleted);

    /// <summary>
    /// 创建时间
    /// </summary>
    public const string CreationTime = nameof(IHasCreationTime.CreationTime);
    
    /// <summary>
    /// 创建者Id
    /// </summary>
    public const string CreatorId = nameof(IMayHaveCreator<int>.CreatorId);
    
        
    /// <summary>
    /// 修改时间
    /// </summary>
    public const string ModificationTime = nameof(IHasModificationTime.LastModificationTime);
    
    /// <summary>
    /// 修改者ID
    /// </summary>
    public const string ModifierId = nameof(IHasModifierId<int>.LastModifierId);
    
    /// <summary>
    /// 删除Id
    /// </summary>
    public const string DeleterId = nameof(IHasDeleterId<int>.DeleterId);
    
    /// <summary>
    /// 删除时间
    /// </summary>
    public const string DeletionTime = nameof(IHasDeletionTime.DeletionTime);
}
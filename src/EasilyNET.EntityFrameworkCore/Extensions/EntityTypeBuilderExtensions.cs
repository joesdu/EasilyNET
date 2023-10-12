namespace EasilyNET.EntityFrameworkCore.Extensions;

/// <summary>
/// 实体构建器扩展
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// 按约定配置
    /// </summary>
    /// <param name="b"></param>
    public static void ConfigureByConvention(this EntityTypeBuilder b)
    {
        // builder.Property(o => o.CreatorId);
        // builder.Property(o => o.CreationTime);
        // builder.Property(o => o.LastModificationTime);
        // builder.Property(o => o.LastModifierId);
        // builder.Property(o => o.DeleterId);
        // builder.Property(o => o.DeletionTime);
        var clrType = b.Metadata.ClrType;
        if (clrType.IsDeriveClassFrom<IHasCreationTime>())
        {
            b.Property(EFCoreShare.CreationTime).IsRequired();
        }
        if (clrType.IsDeriveClassFrom(typeof(IMayHaveCreator<>)))
        {
            b.Property(EFCoreShare.CreatorId).IsRequired(false);
        }
        if (clrType.IsDeriveClassFrom<IHasModificationTime>())
        {
            b.Property(EFCoreShare.ModificationTime).IsRequired(false);
        }
        TryConfigureSoftDelete(b);
        if (clrType.IsDeriveClassFrom(typeof(IHasModifierId<>)))
        {
            b.Property(EFCoreShare.ModifierId).IsRequired(false);
        }
        if (clrType.IsDeriveClassFrom(typeof(IHasDeleterId<>)))
        {
            b.Property(EFCoreShare.DeleterId).IsRequired(false);
        }
        if (clrType.IsDeriveClassFrom<IHasDeletionTime>())
        {
            b.Property(EFCoreShare.DeletionTime).IsRequired(false);
        }
    }

    /// <summary>
    /// 配置软删除
    /// </summary>
    /// <param name="b"></param>
    /// <typeparam name="T">动态实体</typeparam>
    public static void ConfigureSoftDelete<T>(this EntityTypeBuilder<T> b)
        where T : class
    {
        if (b.Metadata.ClrType.IsDeriveClassFrom<IHasSoftDelete>())
        {
            b.Property<bool>(EFCoreShare.IsDeleted).IsRequired().HasDefaultValue(false);
            Expression<Func<T, bool>> expression = e => !EF.Property<bool>(e, "IsDeleted");
            b.HasQueryFilter(expression);
        }
    }

    /// <summary>
    /// 配置软删除
    /// </summary>
    /// <param name="b"></param>
    public static void TryConfigureSoftDelete(this EntityTypeBuilder b)
    {
        if (b.Metadata.ClrType.IsDeriveClassFrom<IHasSoftDelete>())
        {
            b.Property<bool>(EFCoreShare.IsDeleted).IsRequired().HasDefaultValue(false);
        }
    }
}
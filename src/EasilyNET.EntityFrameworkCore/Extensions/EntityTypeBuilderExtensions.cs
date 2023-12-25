// ReSharper disable MemberCanBePrivate.Global

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
        TryConfigureHaveCreator(b);
        TryConfigureModifierId(b);
        TryConfigureSoftDelete(b);
    }

    /// <summary>
    /// 配置软删除
    /// </summary>
    /// <param name="b"></param>
    /// <typeparam name="T">动态实体</typeparam>
    public static void ConfigureSoftDelete<T>(this EntityTypeBuilder<T> b)
        where T : class
    {
        if (!b.Metadata.ClrType.IsDeriveClassFrom<IHasSoftDelete>()) return;
        b.Property<bool>(EFCoreShare.IsDeleted).IsRequired().HasDefaultValue(false);
        Expression<Func<T, bool>> expression = e => !EF.Property<bool>(e, EFCoreShare.IsDeleted);
        b.HasQueryFilter(expression);
    }

    /// <summary>
    /// 配置软删除
    /// </summary>
    /// <param name="b"></param>
    public static void TryConfigureSoftDelete(this EntityTypeBuilder b)
    {
        if (!b.Metadata.ClrType.IsDeriveClassFrom<IHasSoftDelete>()) return;
        b.Property<bool>(EFCoreShare.IsDeleted).IsRequired().HasDefaultValue(false);
        TryConfigureDeleterId(b);
        TryConfigureDeletionTime(b);
    }

    /// <summary>
    /// 配置创建时间
    /// </summary>
    /// <param name="b"></param>
    public static void TryConfigureCreationTime(this EntityTypeBuilder b)
    {
        if (b.Metadata.ClrType.IsDeriveClassFrom<IHasCreationTime>())
        {
            b.Property(EFCoreShare.CreationTime).IsRequired();
        }
    }

    /// <summary>
    /// 配置创建者
    /// </summary>
    /// <param name="b"></param>
    public static void TryConfigureHaveCreator(this EntityTypeBuilder b)
    {
        if (!b.Metadata.ClrType.IsDeriveClassFrom(typeof(IMayHaveCreator<>))) return;
        b.Property(EFCoreShare.CreatorId).IsRequired(false);
        TryConfigureCreationTime(b);
    }

    /// <summary>
    /// 配置修改时间
    /// </summary>
    /// <param name="b"></param>
    public static void TryConfigureModificationTime(this EntityTypeBuilder b)
    {
        if (b.Metadata.ClrType.IsDeriveClassFrom<IHasModificationTime>())
        {
            b.Property(EFCoreShare.ModificationTime).IsRequired(false);
        }
    }

    /// <summary>
    /// 配置修改者ID
    /// </summary>
    /// <param name="b"></param>
    public static void TryConfigureModifierId(this EntityTypeBuilder b)
    {
        if (!b.Metadata.ClrType.IsDeriveClassFrom(typeof(IHasModifierId<>))) return;
        b.Property(EFCoreShare.ModifierId).IsRequired(false);
        TryConfigureModificationTime(b);
    }

    /// <summary>
    /// 配置删除者ID
    /// </summary>
    /// <param name="b"></param>
    public static void TryConfigureDeleterId(this EntityTypeBuilder b)
    {
        if (b.Metadata.ClrType.IsDeriveClassFrom(typeof(IHasDeleterId<>)))
        {
            b.Property(EFCoreShare.DeleterId).IsRequired(false);
        }
    }

    /// <summary>
    /// 配置删除时间
    /// </summary>
    /// <param name="b"></param>
    public static void TryConfigureDeletionTime(this EntityTypeBuilder b)
    {
        if (b.Metadata.ClrType.IsDeriveClassFrom<IHasDeletionTime>())
        {
            b.Property(EFCoreShare.DeletionTime).IsRequired(false);
        }
    }
}
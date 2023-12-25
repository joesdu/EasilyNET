// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.EntityFrameworkCore.Extensions;

/// <summary>
/// 实体扩展
/// </summary>
public static class EntityEntryExtensions
{
    /// <summary>
    /// 属性是否为null
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="propertyName">属性名</param>
    /// <returns>如果是还回ture,否则false</returns>
    public static bool HasProperty(this IEntityType entityType, string propertyName)
    {
        propertyName.NotNullOrEmpty(nameof(propertyName));
        return entityType.FindProperty(propertyName) is null;
    }

    /// <summary>
    /// 属性是否为null
    /// </summary>
    /// <param name="entityType"></param>
    /// <param name="propertyName"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    public static bool HasProperty(this IEntityType entityType, string propertyName, out IProperty? property)
    {
        propertyName.NotNullOrEmpty(nameof(propertyName));
        property = entityType.FindProperty(propertyName);
        return property is null;
    }

    /// <summary>
    /// 设置属性当前值
    /// </summary>
    /// <param name="entityEntry">实体</param>
    /// <param name="propertyName">要设置属性名</param>
    /// <param name="value">值</param>
    public static void SetCurrentValue(this EntityEntry entityEntry, string propertyName, object? value = default)
    {
        entityEntry.NotNull(nameof(EntityEntry));
        if (!entityEntry.Metadata.HasProperty(propertyName) && value is not null)
        {
            entityEntry.CurrentValues[propertyName] = value;
        }
    }

    /// <summary>
    /// 设置当前设置值，设置时候会类型类型
    /// </summary>
    /// <param name="entityEntry">实体</param>
    /// <param name="propertyName">要设置属性名</param>
    /// <param name="value">值</param>
    public static void SetPropertyValue(this EntityEntry entityEntry, string propertyName, object? value)
    {
        entityEntry.NotNull(nameof(EntityEntry));
        if (!entityEntry.Metadata.HasProperty(propertyName, out var property))
        {
            var type = property!.ClrType;
            entityEntry.Property(propertyName).CurrentValue = value.ChangeType(type);
        }
    }
}
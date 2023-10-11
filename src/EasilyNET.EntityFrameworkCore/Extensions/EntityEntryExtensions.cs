

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
    public static bool HasProperty(this IEntityType entityType,string propertyName)
    {
        propertyName.NotNullOrEmpty(nameof(propertyName));
        return entityType.FindProperty(propertyName) is null ? true : false;
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
}
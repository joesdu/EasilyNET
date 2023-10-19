namespace EasilyNET.EntityFrameworkCore.Extensions;

/// <summary>
/// 字段绑定扩展
/// </summary>
public static class PropertyBuilderExtension
{
    /// <summary>
    /// 使用雪花ID生成器
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TEntityId"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static PropertyBuilder<TEntityId> UseSnowFlakeValueGenerator<TEntityId>(
        this PropertyBuilder<TEntityId> builder)
        where TEntityId : IEquatable<long>
    {
        builder = builder ?? throw new ArgumentNullException(nameof(builder));

        return builder.HasValueGenerator<SnowflakeIdValueGenerator<TEntityId>>();
    } 
}
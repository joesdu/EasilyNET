namespace EasilyNET.EntityFrameworkCore;

/// <summary>
/// 雪花值生成
/// </summary>
public class SnowflakeIdValueGenerator<TEntityId> : ValueGenerator<long>
    where TEntityId : IEquatable<long>
{
    /// <summary>
    /// </summary>
    public SnowflakeIdValueGenerator() { }

    /// <inheritdoc />
    public override bool GeneratesTemporaryValues => false;

    /// <summary>
    /// 生成值
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    public override long Next(EntityEntry entry) => SnowFlakeId.Default.NextId();
}
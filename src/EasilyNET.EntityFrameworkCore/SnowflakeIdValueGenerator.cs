using EasilyNET.Core.BaseType;

namespace EasilyNET.EntityFrameworkCore;

/// <summary>
/// 雪花值生成
/// </summary>
public class SnowflakeIdValueGenerator<TEntityId> : ValueGenerator<long>
    where TEntityId : IEquatable<long>
{
    /// <summary>
    /// 
    /// </summary>
    public SnowflakeIdValueGenerator() { }

    /// <summary>
    /// 生成值
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    public override long Next(EntityEntry entry)
    {
        return SnowFlakeId.Default.NextId();
    }

    /// <inheritdoc />
    public override bool GeneratesTemporaryValues => false;
}
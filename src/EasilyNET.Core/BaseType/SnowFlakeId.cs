using Yitter.IdGenerator;

namespace EasilyNET.Core.BaseType;
using  YitterId=Yitter.IdGenerator.IIdGenerator;
/// <summary>
/// 雪花ID
/// </summary>
public static class SnowFlakeId
{
    /// <summary>
    /// 延迟默认
    /// </summary>
    private static Lazy<ISnowFlakeId> lazyDefault = new Lazy<ISnowFlakeId>(() => new SnowFlakeIdImplementation());

    /// <summary>
    /// 得到默认值
    /// </summary>
    public static ISnowFlakeId Default => lazyDefault.Value;
}

/// <summary>
///  雪花ID接口实现
/// </summary>
internal class SnowFlakeIdImplementation : ISnowFlakeId
{
    
   
    static Lazy<YitterId> Default = new Lazy<IIdGenerator>(new DefaultIdGenerator(new IdGeneratorOptions(1)));
    
    /// <inheritdoc />
    public long NextId()
    {
        return Default.Value.NewLong();
    }
}

/// <summary>
/// 雪花ID接口
/// </summary>
public interface ISnowFlakeId
{


    /// <summary>
    /// 得到下一个Id
    /// </summary>
    /// <returns></returns>
    long NextId();
}